/*
Helper.cs
16.01.2022 1:35:02
Alexey Sedoykin
*/

namespace CheckFlexLMLicenseStatus
{
    /// <summary>
    /// Defines the <see cref="LicenseStatusResponce" />.
    /// </summary>
    public class LicenseStatusResponce
    {
        /// <summary>
        /// Gets or sets the licenseStatus.
        /// </summary>
        public string licenseStatus { get; set; }

        /// <summary>
        /// Gets or sets the expiredDate.
        /// </summary>
        public string expiredDate { get; set; }
    }
}
