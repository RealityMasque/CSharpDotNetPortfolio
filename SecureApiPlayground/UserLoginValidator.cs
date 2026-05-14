using FluentValidation;

public class UserLoginValidator : AbstractValidator<UserLoginDto>
{
    public UserLoginValidator()
    {
        RuleFor(x => x.Username).NotEmpty().Length(3, 50);

        RuleFor(x => x.Password).NotEmpty().Length(6, 100);
    }
}