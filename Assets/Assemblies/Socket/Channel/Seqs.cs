#nullable enable
using Larnix.Socket.Payload.Structs;

namespace Larnix.Socket.Channel;

public class Seqs
{
    public Seq SeqNum { get; set; } // last sent seq
    public Seq AckNum { get; set; } // max acked seq (by other)
    public Seq RcvNum { get; set; } // last received seq
}
