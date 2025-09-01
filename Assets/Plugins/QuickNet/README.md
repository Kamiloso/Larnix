# QuickNet Library
Based on a non existent QuickNet protocol.

##

Assembly name: QuickNet
Global namespace: QuickNet

## Public interface

### namespace QuickNet

public static class ArrayUtils
{
    // Concats array more efficiently than simple Concat.Concat.Concat...
    public static T[] MegaConcat<T>(params T[][] arrays);
}

public static class Debug
{
    // Initializes logging functions. Different environments have different logging systems.
    public static void InitLogs(Action<string> log, Action<string> logWarning, Action<string> logError)
}

public static unsafe class EndianUnsafe
{
    // Utilities for fast and efficient conversions between byte[] arrays
    // and primitives

    // Supports primitives, enums and String32/64...1024 structs
    // Reverses array on big endian
    // If some unmanaged struct doesn't need to be reversed, you can attach IIgnoreEndianness
    // and it will be allowed to use in this class (be careful with it, big endian utilities will be ignored then for that struct)

    public static byte[] GetBytes<T>(T value) where T : unmanaged;
    public static T FromBytes<T>(byte[] bytes, int startIndex = 0) where T : unmanaged
    public static byte[] ArrayGetBytes<T>(T[] values) where T : unmanaged
    public static T[] ArrayFromBytes<T>(byte[] bytes, int count, int startIndex = 0) where T : unmanaged
}

// Just a marker for EndianUnsafe
public interface IIgnoresEndianness;

public static class FileManager
{
    // Offers atomic methods to write / read to / fro files

    // these methods do exactly how they are called
    // filename shouldn't start with '_'
    // if writing file is corrupted, it will automatically revert to the backup
    public static string Read(string path, string filename);
    public static void Write(string path, string filename, string text);
    public static byte[] ReadBinary(string path, string filename);
    public static void WriteBinary(string path, string filename, byte[] bytes);

    // delete a file and all its backups (non-atomic, some garbage may remain during power failures etc.)
    public static void Delete(string path, string filename);

    // if directory not exist, create it
    public static void EnsureDirectory(string path);
}

public class InternetID
{
    // Wrapper for IPAddress built for per-IP-limiting dictionaries.
    // It implements ==, !=, .Equals and .GetHashCode in such way, that
    // IP addresses from the same subnet (represented by mask arguments) will be Equal.
    // Always the smaller subnet is chosen during comparison.
    // Example:
    // MaskIPv4 = 24
    // 192.168.0.1 == 192.168.0.240
    // 192.168.0.1 != 192.168.1.13
    // IPv4 and IPv6 addresses are always different
    // Default masks:
    // IPv4: 32 (the whole address)
    // IPv6: 56

    public InternetID(IPAddress address, int maskIPv4, int maskIPv6) // one of the masks arguments is dead, for example if you're using IPv4 address, IPv6 address field can be set to anything
    public static bool operator ==(InternetID left, InternetID right)
    public static bool operator !=(InternetID left, InternetID right)
    public override bool Equals(object obj)
    public override int GetHashCode()
}

// Structs that represent fixed-size strings
// encoded in UTF-16. One such string can fit half the size
// of its bytes characters (for example String32 can fit 16 characters)
// These strings will be cut if too long and NULL characters will be
// trimmed from the end.
// These structs are denesly packet and unmanaged, so they can
// be used in the EndianUnsafe converter
public unsafe struct String32 : IStringStruct
public unsafe struct String64 : IStringStruct
public unsafe struct String128 : IStringStruct
public unsafe struct String256 : IStringStruct
public unsafe struct String512 : IStringStruct
public unsafe struct String1024 : IStringStruct

// interface for StringN structs
public interface IStringStruct : IIgnoresEndianness
{
    // amount of bytes, divide by 2 to get string length
    public int BinarySize { get; }
}

public static class Timestamp
{
    // gets current timestamp
    public static long GetTimestamp();

    // checks if timestamp is no older than [window] ms
    public static bool InTimestamp(long timestamp, long window = 6000);
}

// Functions that the QuickNet library is using to validate
// its internal data. Can be used in forms etc. to ensure
// identical implementations.
public static class Validation
{
    public static bool IsGoodNickname(string nickname) =>
        nickname != null &&
        !nickname.EndsWith('\0') &&
        nickname.Length is >= 3 and <= 16 &&
        nickname.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '_');

    public static bool IsGoodPassword(string password) =>
        password != null &&
        !password.EndsWith('\0') &&
        password.Length is >= 7 and <= 32;

    public static bool IsGoodAuthcode(string authcode) =>
        Processing.Authcode.IsGoodAuthcode(authcode);

    public static bool IsGoodText<T>(string message) where T : IStringStruct, new() =>
        message != null &&
        !message.EndsWith('\0') &&
        message.Length <= new T().BinarySize / 2;
}

### namespace QuickNet.Processing

public static class Hasher
{
    // Utilities for hashing passwords
    // Contains both sync and async methods for elasticity
    // These are the same methods which QuickNet uses internally
    // Passwords are salted and hashed using 50 000 iterations of SHA-256    

    public static string HashPassword(string password);
    public static bool VerifyPassword(string password, string storedSaltedHash);
    public static Task<string> HashPasswordAsync(string password);
    public static Task<bool> VerifyPasswordAsync(string password, string storedSaltedHash);


    // Checks if executing VerifyPassword/VerifyPasswordAsync with
    // the same parameters will have to compute the whole hashing proccess
    // or will just take data from cache.

    public static bool InCache(string password, string storedSaltedHash);
}

### namespace QuickNet.Backend

public class QuickServer : IDisposable
{
    public const string LoopbackOnlyPassword = "SGP_PASSWORD\x01"; // password that will be accepted only if sent from loopback address, can be used to verify localhost accounts without asking player for password

    public readonly ushort Port; // server port
    public readonly ushort MaxClients; // max clients
    public readonly bool IsLoopback; // does server work in loopback-only mode?
    public readonly string DataPath; // directory, where server saves its data
    public readonly uint GameVersion; // game version (information send as server info)
    public readonly string UserText1; // user text 1 (information send as server info), passes validation: Validation.IsGoodText<String256>(string message), max 128 characters and doesn't end with NULL (0x00)
    public readonly string UserText2; // user text 2 (information send as server info), passes validation: Validation.IsGoodText<String256>(string message), max 128 characters and doesn't end with NULL (0x00)
    public readonly string UserText3; // user text 3 (information send as server info), passes validation: Validation.IsGoodText<String256>(string message), max 128 characters and doesn't end with NULL (0x00)

    public readonly long Secret; // server secret known only to server owner and its players, it sits inside authcode
    public readonly string Authcode; // authorization code, unique (in theory) for the server, must be provided by the client to connect. It's based on a server secret and a public RSA key.

    public readonly UserManager UserManager; // user management class, can be accessed to manage user data without client network API
    public readonly RSA KeyRSA; // RSA private key object. It is disposed once the server is closed.

    public QuickServer(ushort port, ushort maxClients, bool isLoopback, string dataPath, uint gameVersion,
        string userText1 = "",
        string userText2 = "",
        string userText3 = ""); // constructor

    public void ConfigureMasks(int maskIPv4, int maskIPv6); // Configure IP-identity masks. Should be executed directly after constructor. If not executed, will fallback to default values (IPv4 = 32, IPv6 = 56). 
    public void ReserveNickname(string nickname); // Adds nickname to the blacklist, which means, that no player can join with it.

    public void ServerTick(float deltaTime); // Executes all tick server actions (data receive, executing subscribtions, etc.) assuming that deltaTime seconds elapsed from previous execution.
    public void Subscribe<T>(Action<T, string> InterpretPacket) where T : Payload, new();
    public void Send(string nickname, Packet packet, bool safemode = true);
    public void Broadcast(Packet packet, bool safemode = true);
    public void FinishConnection(string nickname);
    public void FinishAllConnections();
    public IPEndPoint GetClientEndPoint(string nickname); // returns client's IP and port
    public ushort CountPlayers();
    public float GetPing(string nickname);
    public void Dispose(); // disposes all server resources (connections, key, sockets)
}

public class UserManager
{
    // User Management API. Object can be accessed as
    // a property of QuickNet.Backend.QuickServer called "UserManager"

    public bool UserExists(string username);
    public void AddUser(string username, string hashedPassword);
    public long GetUserID(string username); // a unique and not changing positive long number given to a player during account creation
    public void ChangePassword(string username, string hashedPassword);
    public string GetPasswordHash(string username);

    // Account deletion or username changing is not supported.
    // You can delete the SERVER_DATA_PATH/Accounts/ folder to reset all account data.
    // Modifying files manually is not recommended as this can cause data corruption.
}

### namespace QuickNet.Frontend
