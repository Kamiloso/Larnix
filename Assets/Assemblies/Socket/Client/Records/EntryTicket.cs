#nullable enable
using Larnix.Socket.Payload.Structs;
using Larnix.Socket.Security.KeyStructs;

namespace Larnix.Socket.Client.Records;

internal record EntryTicket(
    long ServerSecret,
    long ChallengeId,
    long Timestamp,
    long RunId,
    FixedRsaPublic RsaPublicKey
    );
