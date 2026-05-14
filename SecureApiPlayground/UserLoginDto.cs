//using System.ComponentModel.DataAnnotations;

public class UserLoginDto
{
    //[Required(ErrorMessage = "Username is required")]
    //[StringLength(50, MinimumLength = 3, ErrorMessage = "Username must be 3-50 characters")]
    public string Username { get; set; } = "";

    //[Required(ErrorMessage = "Password is required")]
    //[StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be 6-100 characters")]
    public string Password { get; set; } = "";
}