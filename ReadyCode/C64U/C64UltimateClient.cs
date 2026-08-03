// Copyright (c) 2026 Moonspace Labs, LLC
// Licensed under the MIT License. See LICENSE in the project root for license information.

using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;

namespace ReadyCode.C64U;

/// <summary>
/// Client for the Commodore 64 Ultimate's REST API.
/// </summary>
public class C64UltimateClient
{
    #region Private Fields

    private static readonly HttpClient _httpClient = new();

    #endregion

    #region Public Methods

    /// <summary>
    /// Uploads a tokenized BASIC program and runs it via POST /v1/runners:load_prg.
    /// </summary>
    /// <param name="baseUrl">Base URL of the C64 Ultimate's REST API.</param>
    /// <param name="prgData">The PRG-format program data to upload.</param>
    /// <returns>The response body returned by the device.</returns>
    public async Task<string> LoadPrgAsync(string baseUrl, byte[] prgData)
    {
        var endpoint = BuildEndpointUri(baseUrl, "v1/runners:load_prg");

        using var content = new ByteArrayContent(prgData);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        using var response = await _httpClient.PostAsync(endpoint, content);
        string body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"The C64 Ultimate returned {(int)response.StatusCode} {response.ReasonPhrase}: {body}");

        return body;
    }

    /// <summary>
    /// Uploads a tokenized BASIC program and runs it immediately via POST /v1/runners:run_prg.
    /// </summary>
    /// <param name="baseUrl">Base URL of the C64 Ultimate's REST API.</param>
    /// <param name="prgData">The PRG-format program data to upload.</param>
    /// <returns>The response body returned by the device.</returns>
    public async Task<string> RunPrgAsync(string baseUrl, byte[] prgData)
    {
        var endpoint = BuildEndpointUri(baseUrl, "v1/runners:run_prg");

        using var content = new ByteArrayContent(prgData);
        content.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");

        using var response = await _httpClient.PostAsync(endpoint, content);
        string body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"The C64 Ultimate returned {(int)response.StatusCode} {response.ReasonPhrase}: {body}");

        return body;
    }

    /// <summary>
    /// Retrieves basic device information via GET /v1/info.
    /// </summary>
    /// <param name="baseUrl">Base URL of the C64 Ultimate's REST API.</param>
    /// <returns>The device information reported by the C64 Ultimate.</returns>
    public async Task<C64UInfo> GetInfoAsync(string baseUrl)
    {
        var endpoint = BuildEndpointUri(baseUrl, "v1/info");

        using var response = await _httpClient.GetAsync(endpoint);
        string body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"The C64 Ultimate returned {(int)response.StatusCode} {response.ReasonPhrase}: {body}");

        return JsonSerializer.Deserialize<C64UInfo>(body)
            ?? throw new InvalidOperationException("The C64 Ultimate returned an empty response.");
    }

    /// <summary>
    /// Sends a machine control command via PUT /v1/machine:{action} (reset, reboot, pause, resume, poweroff).
    /// </summary>
    /// <param name="baseUrl">Base URL of the C64 Ultimate's REST API.</param>
    /// <param name="action">The machine action to perform.</param>
    public async Task MachineActionAsync(string baseUrl, string action)
    {
        var endpoint = BuildEndpointUri(baseUrl, $"v1/machine:{action}");

        using var response = await _httpClient.PutAsync(endpoint, null);
        string body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"The C64 Ultimate returned {(int)response.StatusCode} {response.ReasonPhrase}: {body}");
    }

    /// <summary>
    /// Reads bytes directly from machine memory via GET /v1/machine:readmem.
    /// </summary>
    /// <param name="baseUrl">Base URL of the C64 Ultimate's REST API.</param>
    /// <param name="address">The machine memory address to read from.</param>
    /// <param name="length">The number of bytes to read.</param>
    /// <returns>The bytes returned by the device.</returns>
    public async Task<byte[]> ReadMemoryAsync(string baseUrl, ushort address, int length)
    {
        string hexAddress = address.ToString("X4");
        var endpoint = BuildEndpointUri(baseUrl, $"v1/machine:readmem?address={hexAddress}&length={length}");

        using var response = await _httpClient.GetAsync(endpoint);
        byte[] bodyBytes = await response.Content.ReadAsByteArrayAsync();
        string bodyText = System.Text.Encoding.UTF8.GetString(bodyBytes);

        if (!response.IsSuccessStatusCode)
            ThrowUltimateRequestException("GET", endpoint, response, bodyText);

        return TryParseReadMemoryJson(bodyText) ?? bodyBytes;
    }

    /// <summary>
    /// Writes bytes directly into C64 memory via PUT /v1/machine:writemem.
    /// </summary>
    /// <param name="baseUrl">Base URL of the C64 Ultimate's REST API.</param>
    /// <param name="address">The C64 memory address to write to.</param>
    /// <param name="data">The bytes to write.</param>
    public async Task WriteMemoryAsync(string baseUrl, ushort address, byte[] data)
    {
        string hexData = Convert.ToHexString(data);
        string hexAddress = address.ToString("X4");
        var endpoint = BuildEndpointUri(baseUrl,
            $"v1/machine:writemem?address={hexAddress}&data={Uri.EscapeDataString(hexData)}");

        using var response = await _httpClient.PutAsync(endpoint, null);
        string body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            ThrowUltimateRequestException("PUT", endpoint, response, body, data);
    }

    /// <summary>
    /// Retrieves the status of all drives via GET /v1/drives.
    /// </summary>
    /// <param name="baseUrl">Base URL of the C64 Ultimate's REST API.</param>
    /// <returns>The status of each drive reported by the device.</returns>
    public async Task<List<C64UDriveStatus>> GetDrivesAsync(string baseUrl)
    {
        var endpoint = BuildEndpointUri(baseUrl, "v1/drives");

        using var response = await _httpClient.GetAsync(endpoint);
        string body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"The C64 Ultimate returned {(int)response.StatusCode} {response.ReasonPhrase}: {body}");

        var drives = new List<C64UDriveStatus>();
        using var doc = JsonDocument.Parse(body);

        // Each element of the "drives" array is a single-property object whose property name
        // is the drive id (e.g. "a", "b", "IEC Drive") and whose value holds that drive's fields.
        if (doc.RootElement.TryGetProperty("drives", out var drivesArray))
        {
            foreach (var entry in drivesArray.EnumerateArray())
            {
                foreach (var drive in entry.EnumerateObject())
                {
                    drives.Add(new C64UDriveStatus
                    {
                        Id = drive.Name,
                        Enabled = drive.Value.TryGetProperty("enabled", out var enabled) && enabled.GetBoolean(),
                        Type = drive.Value.TryGetProperty("type", out var type) ? type.GetString() : null,
                        DeviceNumber = TryGetDriveDeviceNumber(drive.Value),
                        ImageFile = drive.Value.TryGetProperty("image_file", out var imageFile) ? imageFile.GetString() ?? "" : "",
                    });
                }
            }
        }

        return drives;
    }

    /// <summary>
    /// Mounts a disk image already on the device's storage to the given drive via
    /// PUT /v1/drives/{driveId}:mount.
    /// </summary>
    /// <param name="baseUrl">Base URL of the C64 Ultimate's REST API.</param>
    /// <param name="driveId">The drive to mount to (e.g. "a", "b").</param>
    /// <param name="imagePath">The full path of the disk image on the device, as returned by the FTP explorer.</param>
    public async Task MountDriveAsync(string baseUrl, string driveId, string imagePath)
    {
        var endpoint = BuildEndpointUri(baseUrl, $"v1/drives/{driveId}:mount?image={Uri.EscapeDataString(imagePath)}");

        using var response = await _httpClient.PutAsync(endpoint, null);
        string body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"The C64 Ultimate returned {(int)response.StatusCode} {response.ReasonPhrase}: {body}");
    }

    /// <summary>
    /// Ejects the disk image currently mounted on the given drive via PUT /v1/drives/{driveId}:remove.
    /// </summary>
    /// <param name="baseUrl">Base URL of the C64 Ultimate's REST API.</param>
    /// <param name="driveId">The drive to eject (e.g. "a", "b").</param>
    public async Task RemoveDriveAsync(string baseUrl, string driveId)
    {
        var endpoint = BuildEndpointUri(baseUrl, $"v1/drives/{driveId}:remove");

        using var response = await _httpClient.PutAsync(endpoint, null);
        string body = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            throw new HttpRequestException($"The C64 Ultimate returned {(int)response.StatusCode} {response.ReasonPhrase}: {body}");
    }

    #endregion

    #region Private Methods

    private static int? TryGetDriveDeviceNumber(JsonElement drive)
    {
        foreach (string propertyName in new[] { "device_number", "device", "unit", "iec_address", "address" })
        {
            if (!drive.TryGetProperty(propertyName, out var value))
                continue;

            if (value.ValueKind == JsonValueKind.Number && value.TryGetInt32(out int number))
                return number;

            if (value.ValueKind == JsonValueKind.String && int.TryParse(value.GetString(), out number))
                return number;
        }

        return null;
    }

    private static byte[]? TryParseReadMemoryJson(string body)
    {
        try
        {
            using var doc = JsonDocument.Parse(body);
            if (doc.RootElement.TryGetProperty("data", out var dataElement))
            {
                if (dataElement.ValueKind == JsonValueKind.String)
                    return Convert.FromHexString(dataElement.GetString()!.Replace(" ", "").Replace("-", ""));

                if (dataElement.ValueKind == JsonValueKind.Array)
                    return dataElement.EnumerateArray().Select(e => (byte)e.GetInt32()).ToArray();
            }
        }
        catch
        {
            // If the device returns raw bytes or an unexpected JSON shape, let the caller see the
            // original bytes instead of failing during diagnostics/probing.
        }

        return null;
    }

    private static void ThrowUltimateRequestException(string method, Uri endpoint, HttpResponseMessage response, string body, byte[]? requestBody = null)
    {
        string requestDetails = $"{method} {endpoint}";
        if (requestBody != null)
        {
            string hex = BitConverter.ToString(requestBody).Replace("-", " ");
            requestDetails += $"\nRequest body ({requestBody.Length} bytes): {hex}";
        }

        throw new HttpRequestException(
            $"The C64 Ultimate returned {(int)response.StatusCode} {response.ReasonPhrase}.\n\n" +
            $"Request:\n{requestDetails}\n\n" +
            $"Response body:\n{body}");
    }

    private static Uri BuildEndpointUri(string baseUrl, string path)
    {
        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new InvalidOperationException("The C64 Ultimate URL has not been configured. Set it in Settings - Preferences.");

        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri))
            throw new InvalidOperationException($"'{baseUrl}' is not a valid URL.");

        // Ensure the base URI is treated as a directory so the endpoint path is appended, not replaced.
        if (!baseUri.AbsoluteUri.EndsWith('/'))
            baseUri = new Uri(baseUri.AbsoluteUri + "/");

        return new Uri(baseUri, path);
    }

    #endregion
}
