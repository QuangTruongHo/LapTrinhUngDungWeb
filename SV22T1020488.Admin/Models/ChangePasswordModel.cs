namespace SV22T1020488.Admin.Models
{
    public class ChangePasswordModel
    {
        public string OldPassword { get; set; } = "";
        public string NewPassword { get; set; } = "";
        public string ConfirmPassword { get; set; } = "";
    }
}