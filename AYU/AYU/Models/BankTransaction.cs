using ProtoBuf;
using System.ComponentModel.DataAnnotations.Schema;

namespace AYU.Models
{
    [ProtoContract]
    public class BankTransaction
    {
        [ProtoMember(1)]
        public int Id { get; set; }

        [ProtoMember(2)]
        public Guid TransactionReference { get; set; }

        [ProtoMember(3)]
        public string SenderAccount { get; set; }

        [ProtoMember(4)]
        public string ReceiverAccount { get; set; }

        [ProtoMember(5)]
        [Column(TypeName = "decimal(18,2)")]
        public decimal Amount { get; set; }

        [ProtoMember(6)]
        public string Currency { get; set; }

        [ProtoMember(7)]
        public DateTime TransactionDate { get; set; }

        [ProtoMember(8)]
        public string Description { get; set; }

        [ProtoMember(9)]
        public bool IsSuccessful { get; set; }
    }
}