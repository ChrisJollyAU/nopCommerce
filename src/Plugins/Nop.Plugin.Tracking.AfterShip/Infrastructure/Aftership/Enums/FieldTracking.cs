/* 
 * This code is taken form AfterShip's GitHub (https://github.com/AfterShip/aftership-sdk-net)
 * and slightly modified for our coding standards. 
 */

using System.ComponentModel;

namespace Nop.Plugin.Tracking.AfterShip.Infrastructure.Aftership.Enums
{
    public enum FieldTracking
    {
        [Description("id")]
        Id,
        [Description("created_at")]
        CreatedAt,
        [Description("updated_at")]
        UpdatedAt,
        [Description("tracking_number")]
        TrackingNumber,
        [Description("slug")]
        Slug,
        [Description("active")]
        Active,
        [Description("custom_fields")]
        CustomFields,
        [Description("customer_name")]
        CustomerName,
        [Description("destination_country_iso3")]
        DestinationCountryIso3,
        [Description("emails")]
        Emails,
        [Description("expected_delivery")]
        ExpectedDelivery,
        [Description("order_id")]
        OrderId,
        [Description("order_id_path")]
        OrderIdPath,
        [Description("origin_country_iso3")]
        OriginCountryIso3,
        [Description("shipment_package_count")]
        ShipmentPackageCount,
        [Description("shipment_type")]
        ShipmentType,
        [Description("signed_by")]
        SignedBy,
        [Description("smses")]
        Phones,
        [Description("source")]
        Source,
        [Description("tag")]
        Tag,
        [Description("title")]
        Title,
        [Description("tracked_count")]
        TrackedCount,
        [Description("checkpoints")]
        Checkpoints
    }
}