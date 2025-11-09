using Base.DAL.Models;
using Base.Shared.DTOs;
using Microsoft.AspNetCore.Identity;
using System.Threading.Tasks;

namespace Base.Services.Interfaces
{
    public interface IAuthService
    {
        /// <summary>
        /// Logins the asynchronous.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns></returns>
        Task<LoginResult> LoginUserAsync(LoginDTO model);

        /// <summary>
        /// Verifies the login asynchronous.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns></returns>
        Task<LoginResult> VerifyLoginAsync(VerifyOtpDTO model);

        /// <summary>
        /// Registers the asynchronous.
        /// </summary>
        /// <param name="model">The model.</param>
        /// <returns>
        /// <see langword="The"/> task that represents the asynchronous operation.
        /// </returns>
        Task RegisterAsync(RegisterDTO model);

        /// <summary>
        /// Sends the otp asynchronous.
        /// </summary>
        /// <param name="email">The email.</param>
        /// <returns>
        /// <see langword="The"/> task that represents the asynchronous operation."
        /// </returns>
        Task SendOtpAsync(string email);

        /// <summary>
        /// Returns true when verification succeeded; false when OTP invalid/expired.
        /// Throws on other failures (e.g. confirm-email failed).
        /// </summary>
        Task<bool> VerifyEmailAsync(VerifyOtpDTO model);


        /// <summary>Resets the password.</summary>
        /// <param name="model">The model.</param>
        /// <returns>
        /// <see langword="if"/> true if reset succeeded; otherwise, false (invalid/expired OTP).
        /// </returns>
        Task<bool> ResetPassword(ResetPasswordDTO model);


        /// <summary>Changes the password asynchronous.</summary>
        /// <param name="userId">The user identifier.</param>
        /// <param name="model">The model.</param>
        /// <returns>
        /// <see langword="if"/> true if change succeeded; otherwise, false (e.g., current password incorrect)."
        /// </returns>
        Task ChangePasswordAsync(string userId, ChangePasswordDTO model);



        /// <summary>Generates the JWT token.</summary>
        /// <param name="user">The user.</param>
        /// <returns>
        /// <see langword="The"/> JWT token as string.
        /// </returns>
        Task<string> GenerateJwtToken(ApplicationUser user);

        #region External Login        
        /// <summary>
        /// Handles the external login asynchronous.
        /// </summary>
        /// <param name="email">The email.</param>
        /// <param name="fullName">The full name.</param>
        /// <returns></returns>
        Task<ExternalLoginResponseDTO> HandleExternalLoginAsync(string email, string fullName);
        #endregion

    }
}