using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Konscious.Security.Cryptography;

namespace EdgeLocker.Services;

public sealed class AppConfig
{
    public byte[] Salt { get; set; } = Array.Empty<byte>();
    public byte[] PasswordHash { get; set; } = Array.Empty<byte>();
    public string GmailAddress { get; set; } = "";
    public string GmailAppPassword { get; set; } = "";
    public string EdgePath { get; set; } = "";
    public string ChromePath { get; set; } = "";
    public bool LockEdge { get; set; } = true;
    public bool LockChrome { get; set; }
    public int AutoRelockMinutes { get; set; } = 5;
    public bool IfeoInstalled { get; set; }
}

public static class ConfigStore
{
    private static readonly string Dir = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "EdgeLocker");
    private static readonly string FilePath = Path.Combine(Dir, "config.dat");

    public static bool Exists() => File.Exists(FilePath);

    public static AppConfig Load()
    {
        var enc = File.ReadAllBytes(FilePath);
        var json = ProtectedData.Unprotect(enc, null, DataProtectionScope.CurrentUser);
        return JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
    }

    public static void Save(AppConfig cfg)
    {
        Directory.CreateDirectory(Dir);
        var json = JsonSerializer.SerializeToUtf8Bytes(cfg);
        var enc = ProtectedData.Protect(json, null, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(FilePath, enc);
    }

    public static (byte[] salt, byte[] hash) HashPassword(string password)
    {
        var salt = RandomNumberGenerator.GetBytes(16);
        return (salt, ComputeHash(password, salt));
    }

    public static byte[] ComputeHash(string password, byte[] salt)
    {
        using var argon = new Argon2id(Encoding.UTF8.GetBytes(password))
        {
            Salt = salt,
            DegreeOfParallelism = 4,
            MemorySize = 65536,
            Iterations = 3
        };
        return argon.GetBytes(32);
    }

    public static bool Verify(string password, AppConfig cfg)
    {
        var h = ComputeHash(password, cfg.Salt);
        return CryptographicOperations.FixedTimeEquals(h, cfg.PasswordHash);
    }
}
