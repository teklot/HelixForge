using System;
using System.Collections.Generic;
using System.Linq;

namespace HelixForge;

/// <summary>
/// Central registry for device lookup by ID and type.
/// Populated at system startup; used for device resolution.
/// </summary>
public sealed class DeviceRegistry
{
    private readonly Dictionary<string, IDevice> _devicesById = new Dictionary<string, IDevice>(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<Type, List<IDevice>> _devicesByType = new Dictionary<Type, List<IDevice>>();

    /// <summary>
    /// Gets the total number of registered devices.
    /// </summary>
    public int Count => _devicesById.Count;

    /// <summary>
    /// Registers a device in the registry.
    /// </summary>
    /// <param name="device">The device to register.</param>
    /// <exception cref="ArgumentNullException">Thrown when device is null.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when a device with the same ID is already registered.
    /// </exception>
    public void Register(IDevice device)
    {
        if (device == null)
            throw new ArgumentNullException(nameof(device));

        if (_devicesById.ContainsKey(device.DeviceId))
            throw new InvalidOperationException($"A device with ID '{device.DeviceId}' is already registered.");

        _devicesById[device.DeviceId] = device;

        var types = GetImplementedDeviceTypes(device);
        foreach (var type in types)
        {
            if (!_devicesByType.TryGetValue(type, out var list))
            {
                list = new List<IDevice>();
                _devicesByType[type] = list;
            }
            list.Add(device);
        }
    }

    /// <summary>
    /// Unregisters a device by its ID.
    /// </summary>
    /// <param name="deviceId">The device ID to remove.</param>
    /// <returns>True if the device was found and removed.</returns>
    public bool Unregister(string deviceId)
    {
        if (!_devicesById.TryGetValue(deviceId, out var device))
            return false;

        _devicesById.Remove(deviceId);

        var types = GetImplementedDeviceTypes(device);
        foreach (var type in types)
        {
            if (_devicesByType.TryGetValue(type, out var list))
            {
                list.Remove(device);
                if (list.Count == 0)
                    _devicesByType.Remove(type);
            }
        }

        return true;
    }

    /// <summary>
    /// Retrieves a device by its unique ID.
    /// </summary>
    /// <param name="deviceId">The device ID.</param>
    /// <returns>The device, or null if not found.</returns>
    public IDevice? GetById(string deviceId)
    {
        _devicesById.TryGetValue(deviceId, out var device);
        return device;
    }

    /// <summary>
    /// Retrieves the first device matching the specified type.
    /// </summary>
    /// <typeparam name="T">The device interface type.</typeparam>
    /// <returns>The first matching device, or null if none found.</returns>
    public T? GetByType<T>() where T : class, IDevice
    {
        if (_devicesByType.TryGetValue(typeof(T), out var list) && list.Count > 0)
            return list[0] as T;
        return null;
    }

    /// <summary>
    /// Retrieves all devices matching the specified type.
    /// </summary>
    /// <typeparam name="T">The device interface type.</typeparam>
    /// <returns>An enumerable of matching devices (empty if none found).</returns>
    public IEnumerable<T> GetAllByType<T>() where T : class, IDevice
    {
        if (_devicesByType.TryGetValue(typeof(T), out var list))
            return list.OfType<T>().ToList();
        return Enumerable.Empty<T>();
    }

    /// <summary>
    /// Gets all registered devices.
    /// </summary>
    /// <returns>An enumerable of all registered devices.</returns>
    public IReadOnlyList<IDevice> GetAllDevices()
    {
        return _devicesById.Values.ToList().AsReadOnly();
    }

    /// <summary>
    /// Checks whether a device with the specified ID is registered.
    /// </summary>
    /// <param name="deviceId">The device ID to check.</param>
    /// <returns>True if the device is registered.</returns>
    public bool Contains(string deviceId)
    {
        return _devicesById.ContainsKey(deviceId);
    }

    private static List<Type> GetImplementedDeviceTypes(IDevice device)
    {
        var types = new List<Type>();

        // Index the concrete type so lookups by concrete class (e.g. SimMotorDevice)
        // resolve, not just interface types.
        var concreteType = device.GetType();
        types.Add(concreteType);

        var interfaceType = concreteType;
        while (interfaceType != null)
        {
            foreach (var iface in interfaceType.GetInterfaces())
            {
                if (iface != typeof(IDisposable) && typeof(IDevice).IsAssignableFrom(iface))
                {
                    if (!types.Contains(iface))
                        types.Add(iface);
                }
            }
            interfaceType = interfaceType.BaseType;
        }

        return types;
    }
}
