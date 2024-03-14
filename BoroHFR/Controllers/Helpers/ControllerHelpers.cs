using BoroHFR.Data;
using BoroHFR.Models;
using BoroHFR.Services;
using BoroHFR.ViewModels.Authentication;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Asn1.Ocsp;
using System;
using Razor.Templating.Core;


namespace BoroHFR.Controllers.Helpers
{
    public static class ControllerHelpers
    {
        /// <summary>
        /// Returns the id of the current user as an <see cref="UserId"/>.
        /// </summary>
        /// <param name="controller"></param>
        /// <returns></returns>
        /// <exception cref="UnauthorizedAccessException"></exception>
        public static UserId GetUserId(this ControllerBase controller)
        {
            var claim = controller.User.FindFirst("Id");
            if (claim is null)
                throw new UnauthorizedAccessException();

            return new UserId(Guid.Parse(claim.Value));
        }

        /// <summary>
        /// This method uses the DbContext actively, save the changes to the db after calling it.
        /// </summary>
        /// <param name="controller"></param>
        /// <param name="dbContext"></param>
        /// <param name="emailService"></param>
        /// <param name="viewService"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public static async Task SendVerificationEmailAsync(this ControllerBase controller, BoroHfrDbContext dbContext, EmailService emailService, User user)
        {
            var emailToken = EmailConfirmationToken.Create(user);
            dbContext.EmailConfirmationTokens.Add(emailToken);
            await dbContext.SaveChangesAsync();
            var confirmUri = new Uri(
                           $"{controller.Request.Scheme}://{controller.Request.Host}{controller.Url.Action("ConfirmEmail", "Authentication", new { token = emailToken.Token.value })}",
                           UriKind.Absolute);
            string msg = await RazorTemplateEngine.RenderPartialAsync("/Views/Email/ConfirmEmail.cshtml", new ConfirmEmailViewModel() { Username = user.Username, ConfirmUrl = confirmUri });
            emailService.Enqueue(new Email("BHR - E-mail cím megerősítése", msg, user.EMail));
        }

        /// <summary>
        /// This method uses the DbContext actively, save the changes to the db after calling it.
        /// </summary>
        /// <param name="controller"></param>
        /// <param name="dbContext"></param>
        /// <param name="emailService"></param>
        /// <param name="viewService"></param>
        /// <param name="user"></param>
        /// <returns></returns>
        public static async Task SendPasswordResetEmailAsync(this ControllerBase controller, BoroHfrDbContext dbContext, EmailService emailService, User user)
        {
            var token = PasswordResetToken.Create(user);
            dbContext.PasswordResetTokens.Add(token);
            await dbContext.SaveChangesAsync();
            var confirmUri = new Uri(
                           $"{controller.Request.Scheme}://{controller.Request.Host}{controller.Url.Action("NewPassword", "Authentication", new { token = token.Token.value })}",
                           UriKind.Absolute);
            string msg = await RazorTemplateEngine.RenderPartialAsync("/Views/Email/ResetPassword.cshtml", new ResetPasswordViewModel() { Username = user.Username, ResetUrl = confirmUri });
            emailService.Enqueue(new Email("BHR - Jelszó visszaállítása", msg, user.EMail));
        }

    }
}
