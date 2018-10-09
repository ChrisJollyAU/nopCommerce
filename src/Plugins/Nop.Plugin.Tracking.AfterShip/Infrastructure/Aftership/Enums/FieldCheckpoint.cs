/* 
 * This code is taken form AfterShip's GitHub (https://github.com/AfterShip/aftership-sdk-net)
 * and slightly modified for our coding standards. 
 */

using System.ComponentModel;

namespace Nop.Plugin.Tracking.AfterShip.Infrastructure.Aftership.Enums
{
    public enum FieldCheckpoint
    {
        [Description("created_at")]
        CreatedAt,
        [Description("checkpoint_time")]
        CheckpointTime,
        [Description("city")]
        City,
        [Description("coordinates")]
        Coordinates,
        [Description("country_iso3")]
        CountryIso3,
        [Description("country_name")]
        CountryName,
        [Description("message")]
        Message,
        [Description("state")]
        State,
        [Description("tag")]
        Tag,
        [Description("zip")]
        Zip
    }
}