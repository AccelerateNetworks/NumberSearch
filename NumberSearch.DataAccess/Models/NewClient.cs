using Dapper;

using Microsoft.AspNetCore.Http;

using Npgsql;

using System;
using System.Linq;
using System.Threading.Tasks;

namespace NumberSearch.DataAccess
{
    public class NewClient
    {
        public Guid NewClientId { get; set; }
        public Guid OrderId { get; set; }
        public string BillingClientId { get; set; } = string.Empty;
        public ExtensionRegistration[] ExtensionRegistrations { get; set; } = [];
        public bool PhoneMenu { get; set; }
        public string PhonesToRingOrMenuDescription { get; set; } = string.Empty;
        public PhoneMenuOption[] PhoneMenuOptions { get; set; } = [];
        public string BusinessHours { get; set; } = string.Empty;
        public string AfterHoursVoicemail { get; set; } = string.Empty;
        public NumberDescription[] NumberDescriptions { get; set; } = [];
        public bool TextingService { get; set; }
        public string TextingServiceName { get; set; } = string.Empty;
        public bool OverheadPaging { get; set; }
        public string OverheadPagingDescription { get; set; } = string.Empty;
        public bool Intercom { get; set; }
        public string IntercomDescription { get; set; } = string.Empty;
        public IntercomRegistration[] IntercomRegistrations { get; set; } = [];
        public bool SpeedDial { get; set; }
        public SpeedDialKey[] SpeedDialKeys { get; set; } = [];
        public bool CustomHoldMusic { get; set; }
        public string HoldMusicDescription { get; set; } = string.Empty;
        public string PhoneOfflineInstructions { get; set; } = string.Empty;
        public FollowMeRegistration[] FollowMeRegistrations { get; set; } = [];
        public DateTime DateUpdated { get; set; }
        public string ISP { get; set; } = string.Empty;
        public DateTime ContractStartDate { get; set; }
        public int ContractCommitmentMonths { get; set; }
        public decimal ContractMonthlyCost { get; set; }
        public int UploadSpeed { get; set; }
        public int DownloadSpeed { get; set; }
        public string ClientRouter { get; set; } = string.Empty;
        public string ClientITVendor { get; set; } = string.Empty;
        // Only used in the form
        public IFormFile? BillImage { get; set; }
        public string BillImagePath { get; set; } = string.Empty;
        public string BillImageFileType { get; set; } = string.Empty;

        public static async Task<NewClient?> GetAsync(Guid newClientId, string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QuerySingleOrDefaultAsync<NewClient?>("SELECT \"NewClientId\", \"OrderId\", \"BillingClientId\", \"PhoneMenu\", \"PhonesToRingOrMenuDescription\", \"BusinessHours\", \"AfterHoursVoicemail\", \"TextingService\", \"TextingServiceName\", \"OverheadPaging\", \"OverheadPagingDescription\", \"Intercom\", \"CustomHoldMusic\", \"HoldMusicDescription\", \"PhoneOfflineInstructions\", \"DateUpdated\", \"SpeedDial\", \"IntercomDescription\", \"ContractStartDate\", \"ISP\", \"ContractCommitmentMonths\", \"UploadSpeed\", \"DownloadSpeed\", \"ContractMonthlyCost\", \"BillImagePath\", \"BillImageFileType\" FROM public.\"NewClients\" " +
                "WHERE \"NewClientId\" = @newClientId", new { newClientId })
                .ConfigureAwait(false);

            return result;
        }

        public static async Task<NewClient?> GetByOrderIdAsync(Guid orderId, string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QuerySingleOrDefaultAsync<NewClient?>("SELECT \"NewClientId\", \"OrderId\", \"BillingClientId\", \"PhoneMenu\", \"PhonesToRingOrMenuDescription\", \"BusinessHours\", \"AfterHoursVoicemail\", \"TextingService\", \"TextingServiceName\", \"OverheadPaging\", \"OverheadPagingDescription\", \"Intercom\", \"CustomHoldMusic\", \"HoldMusicDescription\", \"PhoneOfflineInstructions\", \"DateUpdated\", \"SpeedDial\", \"IntercomDescription\", \"ContractStartDate\", \"ISP\", \"ContractCommitmentMonths\", \"UploadSpeed\", \"DownloadSpeed\", \"ContractMonthlyCost\", \"BillImagePath\", \"BillImageFileType\" FROM public.\"NewClients\" " +
                "WHERE \"OrderId\" = @orderId", new { orderId })
                .ConfigureAwait(false);

            return result;
        }

        public async Task<bool> PostAsync(string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .ExecuteAsync("INSERT INTO public.\"NewClients\" ( \"NewClientId\", \"OrderId\", \"BillingClientId\", \"PhoneMenu\", \"PhonesToRingOrMenuDescription\", \"BusinessHours\", \"AfterHoursVoicemail\", \"TextingService\", \"TextingServiceName\", \"OverheadPaging\", \"OverheadPagingDescription\", \"Intercom\", \"CustomHoldMusic\", \"HoldMusicDescription\", \"PhoneOfflineInstructions\", \"DateUpdated\", \"SpeedDial\", \"IntercomDescription\", \"ContractStartDate\", \"ISP\", \"ContractCommitmentMonths\", \"UploadSpeed\", \"DownloadSpeed\", \"ContractMonthlyCost\", \"BillImagePath\", \"BillImageFileType\") " +
                "VALUES (@NewClientId, @OrderId, @BillingClientId, @PhoneMenu, @PhonesToRingOrMenuDescription, @BusinessHours, @AfterHoursVoicemail, @TextingService, @TextingServiceName, @OverheadPaging, @OverheadPagingDescription, @Intercom, @CustomHoldMusic, @HoldMusicDescription, @PhoneOfflineInstructions, @DateUpdated, @SpeedDial, @IntercomDescription, @ContractStartDate, @ISP, @ContractCommitmentMonths, @UploadSpeed, @DownloadSpeed, @ContractMonthlyCost, @BillImagePath, @BillImageFileType)",
                new { NewClientId, OrderId, BillingClientId, PhoneMenu, PhonesToRingOrMenuDescription, BusinessHours, AfterHoursVoicemail, TextingService, TextingServiceName, OverheadPaging, OverheadPagingDescription, Intercom, CustomHoldMusic, HoldMusicDescription, PhoneOfflineInstructions, DateUpdated, SpeedDial, IntercomDescription, ContractStartDate, ISP, ContractCommitmentMonths, UploadSpeed, DownloadSpeed, ContractMonthlyCost, BillImagePath, BillImageFileType })
                .ConfigureAwait(false);

            if (result == 1)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public async Task<bool> PutAsync(string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .ExecuteAsync("UPDATE public.\"NewClients\" SET \"BillingClientId\" = @BillingClientId, \"PhoneMenu\" = @PhoneMenu, \"PhonesToRingOrMenuDescription\" = @PhonesToRingOrMenuDescription, \"BusinessHours\" = @BusinessHours, \"AfterHoursVoicemail\" = @AfterHoursVoicemail, \"TextingService\" = @TextingService, \"TextingServiceName\" = @TextingServiceName, \"OverheadPaging\" = @OverheadPaging, \"OverheadPagingDescription\" = @OverheadPagingDescription, \"Intercom\" = @Intercom, \"CustomHoldMusic\" = @CustomHoldMusic, \"HoldMusicDescription\" = @HoldMusicDescription, \"PhoneOfflineInstructions\" = @PhoneOfflineInstructions, \"DateUpdated\" = @DateUpdated, \"SpeedDial\" = @SpeedDial, \"IntercomDescription\" = @IntercomDescription, \"ContractStartDate\" = @ContractStartDate, \"ISP\" = @ISP, \"ContractCommitmentMonths\" = @ContractCommitmentMonths, \"UploadSpeed\" = @UploadSpeed, \"DownloadSpeed\" = @DownloadSpeed, \"ContractMonthlyCost\" = @ContractMonthlyCost, \"BillImagePath\" = @BillImagePath, \"BillImageFileType\" = @BillImageFileType " +
                "WHERE \"NewClientId\" = @NewClientId",
                new { NewClientId, BillingClientId, PhoneMenu, PhonesToRingOrMenuDescription, BusinessHours, AfterHoursVoicemail, TextingService, TextingServiceName, OverheadPaging, OverheadPagingDescription, Intercom, CustomHoldMusic, HoldMusicDescription, PhoneOfflineInstructions, DateUpdated, SpeedDial, IntercomDescription, ContractStartDate, ISP, ContractCommitmentMonths, UploadSpeed, DownloadSpeed, ContractMonthlyCost, BillImagePath, BillImageFileType })
                .ConfigureAwait(false);

            if (result == 1)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    public class ExtensionRegistration
    {
        public Guid ExtensionRegistrationId { get; set; }
        public Guid NewClientId { get; set; }
        public int ExtensionNumber { get; set; }
        public string NameOrLocation { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string ModelOfPhone { get; set; } = string.Empty;
        public string OutboundCallerId { get; set; } = string.Empty;
        public DateTime DateUpdated { get; set; }

        public static async Task<ExtensionRegistration[]> GetByNewClientAsync(Guid newClientId, string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryAsync<ExtensionRegistration>("SELECT \"ExtensionRegistrationId\", \"NewClientId\", \"ExtensionNumber\", \"NameOrLocation\", \"Email\", \"ModelOfPhone\", \"OutboundCallerId\", \"DateUpdated\" FROM public.\"ExtensionRegistrations\" " +
                "WHERE \"NewClientId\" = @newClientId", new { newClientId })
                .ConfigureAwait(false);

            return result.ToArray();
        }

        public async Task<bool> PostAsync(string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .ExecuteAsync("INSERT INTO public.\"ExtensionRegistrations\" ( \"ExtensionRegistrationId\", \"NewClientId\", \"ExtensionNumber\", \"NameOrLocation\", \"Email\", \"ModelOfPhone\", \"OutboundCallerId\", \"DateUpdated\" ) " +
                "VALUES ( @ExtensionRegistrationId, @NewClientId, @ExtensionNumber, @NameOrLocation, @Email, @ModelOfPhone, @OutboundCallerId, @DateUpdated )",
                new { ExtensionRegistrationId, NewClientId, ExtensionNumber, NameOrLocation, Email, ModelOfPhone, OutboundCallerId, DateUpdated })
                .ConfigureAwait(false);

            if (result == 1)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public async Task<bool> DeleteAsync(string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .ExecuteAsync("DELETE FROM public.\"ExtensionRegistrations\" WHERE \"ExtensionRegistrationId\" = @ExtensionRegistrationId",
                new { ExtensionRegistrationId })
                .ConfigureAwait(false);

            if (result == 1)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    public class NumberDescription
    {
        public Guid NumberDescriptionId { get; set; }
        public Guid NewClientId { get; set; }
        public string PhoneNumber { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Prefix { get; set; } = string.Empty;
        public DateTime DateUpdated { get; set; }

        public static async Task<NumberDescription[]> GetByNewClientAsync(Guid newClientId, string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryAsync<NumberDescription>("SELECT \"NumberDescriptionId\", \"NewClientId\", \"PhoneNumber\", \"Description\", \"Prefix\", \"DateUpdated\" FROM public.\"NumberDescriptions\" " +
                "WHERE \"NewClientId\" = @newClientId", new { newClientId })
                .ConfigureAwait(false);

            return result.ToArray();
        }

        public async Task<bool> PostAsync(string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .ExecuteAsync("INSERT INTO public.\"NumberDescriptions\" ( \"NumberDescriptionId\", \"NewClientId\", \"PhoneNumber\", \"Description\", \"Prefix\", \"DateUpdated\" ) " +
                "VALUES ( @NumberDescriptionId, @NewClientId, @PhoneNumber, @Description, @Prefix, @DateUpdated)",
                new { NumberDescriptionId, NewClientId, PhoneNumber, Description, Prefix, DateUpdated })
                .ConfigureAwait(false);

            if (result == 1)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public async Task<bool> DeleteAsync(string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .ExecuteAsync("DELETE FROM public.\"NumberDescriptions\" WHERE \"NumberDescriptionId\" = @NumberDescriptionId",
                new { NumberDescriptionId })
                .ConfigureAwait(false);

            if (result == 1)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    public class IntercomRegistration
    {
        public Guid IntercomRegistrationId { get; set; }
        public Guid NewClientId { get; set; }
        public int ExtensionSendingIntercom { get; set; }
        public int ExtensionRecievingIntercom { get; set; }
        public DateTime DateUpdated { get; set; }

        public static async Task<IntercomRegistration[]> GetByNewClientAsync(Guid newClientId, string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryAsync<IntercomRegistration>("SELECT \"IntercomRegistrationId\", \"NewClientId\", \"ExtensionSendingIntercom\", \"ExtensionRecievingIntercom\", \"DateUpdated\" FROM public.\"IntercomRegistrations\" " +
                "WHERE \"NewClientId\" = @newClientId", new { newClientId })
                .ConfigureAwait(false);

            return result.ToArray();
        }

        public async Task<bool> PostAsync(string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .ExecuteAsync("INSERT INTO public.\"IntercomRegistrations\" ( \"IntercomRegistrationId\", \"NewClientId\", \"ExtensionSendingIntercom\", \"ExtensionRecievingIntercom\", \"DateUpdated\" ) " +
                "VALUES ( @IntercomRegistrationId, @NewClientId, @ExtensionSendingIntercom, @ExtensionRecievingIntercom, @DateUpdated )",
                new { IntercomRegistrationId, NewClientId, ExtensionSendingIntercom, ExtensionRecievingIntercom, DateUpdated })
                .ConfigureAwait(false);

            if (result == 1)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public async Task<bool> DeleteAsync(string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .ExecuteAsync("DELETE FROM public.\"IntercomRegistrations\" WHERE \"IntercomRegistrationId\" = @IntercomRegistrationId",
                new { IntercomRegistrationId })
                .ConfigureAwait(false);

            if (result == 1)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    public class SpeedDialKey
    {
        public Guid SpeedDialKeyId { get; set; }
        public Guid NewClientId { get; set; }
        public string NumberOrExtension { get; set; } = string.Empty;
        public string LabelOrName { get; set; } = string.Empty;
        public DateTime DateUpdated { get; set; }

        public static async Task<SpeedDialKey[]> GetByNewClientAsync(Guid newClientId, string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryAsync<SpeedDialKey>("SELECT \"SpeedDialKeyId\", \"NewClientId\", \"NumberOrExtension\", \"LabelOrName\", \"DateUpdated\" FROM public.\"SpeedDialKeys\" " +
                "WHERE \"NewClientId\" = @newClientId", new { newClientId })
                .ConfigureAwait(false);

            return result.ToArray();
        }

        public async Task<bool> PostAsync(string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .ExecuteAsync("INSERT INTO public.\"SpeedDialKeys\" ( \"SpeedDialKeyId\", \"NewClientId\", \"NumberOrExtension\", \"LabelOrName\", \"DateUpdated\" ) " +
                "VALUES ( @SpeedDialKeyId, @NewClientId, @NumberOrExtension, @LabelOrName, @DateUpdated )",
                new { SpeedDialKeyId, NewClientId, NumberOrExtension, LabelOrName, DateUpdated })
                .ConfigureAwait(false);

            if (result == 1)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public async Task<bool> DeleteAsync(string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .ExecuteAsync("DELETE FROM public.\"SpeedDialKeys\" WHERE \"SpeedDialKeyId\" = @SpeedDialKeyId",
                new { SpeedDialKeyId })
                .ConfigureAwait(false);

            if (result == 1)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    public class PhoneMenuOption
    {
        public Guid PhoneMenuOptionId { get; set; }
        public Guid NewClientId { get; set; }
        public string MenuOption { get; set; } = string.Empty;
        public string Destination { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public DateTime DateUpdated { get; set; }

        public static async Task<PhoneMenuOption[]> GetByNewClientAsync(Guid newClientId, string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryAsync<PhoneMenuOption>("SELECT \"PhoneMenuOptionId\", \"NewClientId\", \"MenuOption\", \"Destination\", \"Description\", \"DateUpdated\" FROM public.\"PhoneMenuOptions\" " +
                "WHERE \"NewClientId\" = @newClientId", new { newClientId })
                .ConfigureAwait(false);

            return result.ToArray();
        }

        public async Task<bool> PostAsync(string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .ExecuteAsync("INSERT INTO public.\"PhoneMenuOptions\" (\"PhoneMenuOptionId\", \"NewClientId\", \"MenuOption\", \"Destination\", \"Description\", \"DateUpdated\") " +
                "VALUES(@PhoneMenuOptionId, @NewClientId, @MenuOption, @Destination, @Description, @DateUpdated)",
                new { PhoneMenuOptionId, NewClientId, MenuOption, Destination, Description, DateUpdated })
                .ConfigureAwait(false);

            if (result == 1)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public async Task<bool> DeleteAsync(string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .ExecuteAsync("DELETE FROM public.\"PhoneMenuOptions\" WHERE \"PhoneMenuOptionId\" = @PhoneMenuOptionId",
                new { PhoneMenuOptionId })
                .ConfigureAwait(false);

            if (result == 1)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }

    public class FollowMeRegistration
    {
        public Guid FollowMeRegistrationId { get; set; }
        public Guid NewClientId { get; set; }
        public string NumberOrExtension { get; set; } = string.Empty;
        public string CellPhoneNumber { get; set; } = string.Empty;
        public string UnreachablePhoneNumber { get; set; } = string.Empty;
        public DateTime DateUpdated { get; set; }

        public static async Task<FollowMeRegistration[]> GetByNewClientAsync(Guid newClientId, string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .QueryAsync<FollowMeRegistration>("SELECT \"FollowMeRegistrationId\", \"NewClientId\", \"NumberOrExtension\", \"CellPhoneNumber\", \"UnreachablePhoneNumber\", \"DateUpdated\" FROM public.\"FollowMeRegistrations\" " +
                "WHERE \"NewClientId\" = @newClientId", new { newClientId })
                .ConfigureAwait(false);

            return result.ToArray();
        }

        public async Task<bool> PostAsync(string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .ExecuteAsync("INSERT INTO public.\"FollowMeRegistrations\" ( \"FollowMeRegistrationId\", \"NewClientId\", \"NumberOrExtension\", \"CellPhoneNumber\", \"UnreachablePhoneNumber\", \"DateUpdated\" ) " +
                "VALUES ( @FollowMeRegistrationId, @NewClientId, @NumberOrExtension, @CellPhoneNumber, @UnreachablePhoneNumber, @DateUpdated )",
                new { FollowMeRegistrationId, NewClientId, NumberOrExtension, CellPhoneNumber, UnreachablePhoneNumber, DateUpdated })
                .ConfigureAwait(false);

            if (result == 1)
            {
                return true;
            }
            else
            {
                return false;
            }
        }

        public async Task<bool> DeleteAsync(string connectionString)
        {
            await using var connection = new NpgsqlConnection(connectionString);

            var result = await connection
                .ExecuteAsync("DELETE FROM public.\"FollowMeRegistrations\" WHERE \"FollowMeRegistrationId\" = @FollowMeRegistrationId",
                new { FollowMeRegistrationId })
                .ConfigureAwait(false);

            if (result == 1)
            {
                return true;
            }
            else
            {
                return false;
            }
        }
    }
}
