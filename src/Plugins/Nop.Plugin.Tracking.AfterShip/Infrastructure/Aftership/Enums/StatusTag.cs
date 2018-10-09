/* 
 * This code is taken form AfterShip's GitHub (https://github.com/AfterShip/aftership-sdk-net)
 * and slightly modified for our coding standards. 
 */

using System.ComponentModel;

namespace Nop.Plugin.Tracking.AfterShip.Infrastructure.Aftership.Enums
{
    public enum StatusTag
    {
        [Description("Unknown")]
        Unknown = 0,
        [Description("Pending")]
        Pending = 1,
        [Description("InfoReceived")]
        InfoReceived = 2,
        [Description("InTransit")]
        InTransit = 3,
        [Description("OutForDelivery")]
        OutForDelivery = 4,
        [Description("AttemptFail")]
        AttemptFail = 5,
        [Description("Delivered")]
        Delivered = 6,
        [Description("Exception")]
        Exception = 7,
        [Description("Expired")]
        Expired = 8
    }
}