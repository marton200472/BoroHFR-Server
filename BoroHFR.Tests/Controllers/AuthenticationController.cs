using System.Security.Claims;
using System.Security.Principal;
using BoroHFR.Data;
using BoroHFR.Models;
using BoroHFR.Services;
using BoroHFR.ViewModels.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Moq;
using ControllerContext = Microsoft.AspNetCore.Mvc.ControllerContext;
using BoroHFR.Controllers;
using Moq.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace BoroHFR.Tests.Controllers;

public class AuthenticationController
{
    private readonly BoroHFR.Controllers.AuthenticationController controller;

    public AuthenticationController()
    {
        var dbContextMock = new Mock<BoroHfrDbContext>();
        dbContextMock.Setup();

        var context = new Mock<HttpContext>();
        var request = new Mock<HttpRequest>();
        request.Setup(x => x.Scheme).Returns("https");
        request.Setup(x => x.Host).Returns(HostString.FromUriComponent("localhost:5000"));
        context 
            .Setup(c => c.Request)
            .Returns(request.Object);


        var emailServiceMock = new Mock<EmailService>();
        emailServiceMock.Setup(x => x.Enqueue(It.IsAny<Email>()));

        var loggerMock = new Mock<ILogger<BoroHFR.Controllers.AuthenticationController>>();

        var configMock = new Mock<IConfiguration>();

        controller = new BoroHFR.Controllers.AuthenticationController(dbContextMock.Object, 
            emailServiceMock.Object, loggerMock.Object, configMock.Object);
        controller.ControllerContext = new ControllerContext() { HttpContext = context.Object };
        controller.Url = new UrlHelper();
    }

    class UrlHelper : IUrlHelper
    {
        public string? Action(UrlActionContext actionContext)
        {
            return "/";
        }

        public string? Content(string? contentPath)
        {
            throw new NotImplementedException();
        }

        public bool IsLocalUrl(string? url)
        {
            throw new NotImplementedException();
        }

        public string? RouteUrl(UrlRouteContext routeContext)
        {
            throw new NotImplementedException();
        }

        public string? Link(string? routeName, object? values)
        {
            throw new NotImplementedException();
        }

        public ActionContext ActionContext { get; } = null!;
    }

    

    [Fact]
    public void Index_RedirectToAction()
    {
        var result = controller.Index();
        Assert.IsType<RedirectToActionResult>(result);
    }

    [Fact]
    public void Contributors_View()
    {
        var result = controller.Contributors();
        Assert.IsType<ViewResult>(result);
    }

    #region Login

    [Fact]
    public void Login_Get_View()
    {
        var result = controller.Login();
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task Login_Success()
    {
        var result = await controller
            .Login(new LoginViewModel() 
                    { Username = "test", Password = "apitest1234" },
                null);
        Assert.IsType<SignInResult>(result);
    }

    [Fact]
    public async Task Login_ValidationError()
    {
        controller.ModelState.AddModelError("a", "b");
        var result = await controller.Login(
            new LoginViewModel() { },
                null);
        Assert.IsType<ViewResult>(result);
        var view = (result as ViewResult)!;
        Assert.IsType<LoginViewModel>(view.Model);
        var model = (view.Model as LoginViewModel)!;
        Assert.False(model.BadUsername);
        Assert.False(model.BadPassword);
    }

    [Fact]
    public async Task Login_BadPassword_Failure()
    {
        var result = await controller.Login(
            new LoginViewModel() { 
                    Username = "test", 
                    Password = "testpassword123" },
            null);
        Assert.IsType<ViewResult>(result);
        var view = (result as ViewResult)!;
        Assert.Null(view.ViewName);
        Assert.IsType<LoginViewModel>(view.Model);
        var model = (view.Model as LoginViewModel)!;
        Assert.True(model.BadPassword);
    }

    [Fact]
    public async Task Login_BadUsername_Failure()
    {
        var result = await controller.Login(
                new LoginViewModel() { 
                    Username = "bad_username",
                    Password = "apitest1234" },
                null);
        Assert.IsType<ViewResult>(result);
        var view = (result as ViewResult)!;
        Assert.Null(view.ViewName);
        Assert.IsType<LoginViewModel>(view.Model);
        var model = (view.Model as LoginViewModel)!;
        Assert.True(model.BadUsername);
    }

    [Fact]
    public async Task Login_EmailNotVerified_View()
    {
        var result = await controller.Login(
            new LoginViewModel() {
                Username = "test2", 
                Password = "apitest1234" }, 
            null);
        Assert.IsType<ViewResult>(result);
        var view = (result as ViewResult)!;
        Assert.Equal("EmailConfirmationNotification", view.ViewName);
        Assert.IsType<EmailConfirmationNotificationViewModel>(view.Model);
    }

    #endregion

    #region Register

    [Fact]
    public void Register_View()
    {
        var result = controller.Register();
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task Register_Success()
    {
        var result = await controller.Register(new RegisterViewModel()
        {
            Token = "validtoken",
            AcceptPrivacyPolicy = true, 
            Username = "test3", 
            EMail = "test3@test.com",
            Password = "Boronkay01", PasswordAgain = "Boronkay01"
        });
        Assert.IsType<ViewResult>(result);
        var view = (result as ViewResult)!;
        Assert.Equal("EmailConfirmationNotification", view.ViewName);
    }

    [Fact]
    public async Task Register_ValidationError()
    {
        controller.ModelState.AddModelError("a", "b");
        var result = await controller.Register(new RegisterViewModel());
        Assert.IsType<ViewResult>(result);
        var view = (result as ViewResult)!;
        Assert.IsType<RegisterViewModel>(view.Model);
        var model = (view.Model as RegisterViewModel)!;
        Assert.False(model.DidNotAcceptPrivacyPolicy);
        Assert.False(model.BadToken);
        Assert.False(model.UsernameTaken);
        Assert.False(model.EMailTaken);
    }

    [Fact]
    public async Task Register_DidNotAcceptPrivacyPolicy()
    {
        var result = await controller.Register(new RegisterViewModel()
        {
            Token = "validtoken",
            AcceptPrivacyPolicy = false,
            Username = "test3",
            EMail = "test3@test.com",
            Password = "Boronkay01",
            PasswordAgain = "Boronkay01"
        });
        Assert.IsType<ViewResult>(result);
        var view = (result as ViewResult)!;
        Assert.Null(view.ViewName);
        Assert.IsType<RegisterViewModel>(view.Model);
        var model = (view.Model as RegisterViewModel)!;
        Assert.True(model.DidNotAcceptPrivacyPolicy);
    }

    [Fact]
    public async Task Register_UsernameTaken()
    {
        var result = await controller.Register(new RegisterViewModel()
        {
            Token = "validtoken",
            AcceptPrivacyPolicy = true,
            Username = "test",
            EMail = "test3@test.com",
            Password = "Boronkay01",
            PasswordAgain = "Boronkay01"
        });
        Assert.IsType<ViewResult>(result);
        var view = (result as ViewResult)!;
        Assert.Null(view.ViewName);
        Assert.IsType<RegisterViewModel>(view.Model);
        var model = (view.Model as RegisterViewModel)!;
        Assert.True(model.UsernameTaken);
    }

    [Fact]
    public async Task Register_EmailTaken()
    {
        var result = await controller.Register(new RegisterViewModel()
        {
            Token = "validtoken",
            AcceptPrivacyPolicy = true,
            Username = "test3",
            EMail = "test@test.com",
            Password = "Boronkay01",
            PasswordAgain = "Boronkay01"
        });
        Assert.IsType<ViewResult>(result);
        var view = (result as ViewResult)!;
        Assert.Null(view.ViewName);
        Assert.IsType<RegisterViewModel>(view.Model);
        var model = (view.Model as RegisterViewModel)!;
        Assert.True(model.EMailTaken);
    }


    [Theory]
    [InlineData("")]
    [InlineData("aaaaaaaaaa")]
    [InlineData("invalidtkn")]
    [InlineData("usedtokenn")]
    public async Task Register_BadToken(string token)
    {
        var result = await controller.Register(new RegisterViewModel()
        {
            Token = token,
            AcceptPrivacyPolicy = true,
            Username = "test3",
            EMail = "test3@test.com",
            Password = "Boronkay01",
            PasswordAgain = "Boronkay01"
        });
        Assert.IsType<ViewResult>(result);
        var view = (result as ViewResult)!;
        Assert.Null(view.ViewName);
        Assert.IsType<RegisterViewModel>(view.Model);
        var model = (view.Model as RegisterViewModel)!;
        Assert.True(model.BadToken);
    }

    #endregion

    [Fact]
    public void Logout_Success()
    {
        var result = controller.Logout();
        Assert.IsType<SignOutResult>(result);
        var res = (result as SignOutResult)!;
        Assert.NotNull(res.Properties);
        //returns / because of the mocked Url.Action
        Assert.Equal("/",res.Properties.RedirectUri);
    }

    [Fact]
    public void PasswordReset_View()
    {
        var result = controller.ResetPassword();
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task ResetPassword_Success()
    {
        var result = await controller.ResetPassword(new PasswordResetRequestViewModel() {EMail = "test@test.com"});
        Assert.IsType<ViewResult>(result);
        var view = (result as ViewResult)!;
        Assert.Equal("PasswordResetEmailSent",view.ViewName);
    }

    [Fact]
    public async Task ResetPassword_UserNotFound()
    {
        var result = await controller.ResetPassword(new PasswordResetRequestViewModel() { EMail = "invalid@test.com" });
        Assert.IsType<ViewResult>(result);
        var view = (result as ViewResult)!;
        Assert.Equal("PasswordResetEmailSent", view.ViewName);
    }

    [Fact]
    public async Task ResetPassword_ValidationError()
    {
        controller.ModelState.AddModelError("a","b");
        var result = await controller.ResetPassword(new PasswordResetRequestViewModel() { EMail = "invalid@test.com" });
        Assert.IsType<ViewResult>(result);
        var view = (result as ViewResult)!;
        Assert.Null(view.ViewName);
        Assert.IsType<PasswordResetRequestViewModel>(view.Model);
    }

    [Fact]
    public async Task NewPassword_View_Success()
    {
        var result = await controller.NewPassword(new PasswordResetTokenId(Guid.Parse("cc2deed3-94c3-4cf8-91ca-f2419f8c5bd2")));
        Assert.IsType<ViewResult>(result);
        var view = (result as ViewResult)!;
        Assert.Null(view.ViewName);
    }

    [Fact]
    public async Task NewPassword_View_NotFound()
    {
        var result = await controller.NewPassword(new PasswordResetTokenId(Guid.Empty));
        Assert.IsType<ViewResult>(result);
        var view = (result as ViewResult)!;
        Assert.Equal("NewPasswordFailed", view.ViewName);
    }

    [Fact]
    public async Task NewPassword_Post_NotFound()
    {
        var result = await controller.NewPassword(new PasswordResetTokenId(Guid.Empty), new NewPasswordViewModel() {NewPassword = "Boronkay01", NewPasswordAgain = "Boronkay01"});
        Assert.IsType<ViewResult>(result);
        var view = (result as ViewResult)!;
        Assert.Equal("NewPasswordFailed", view.ViewName);
    }

    [Fact]
    public async Task NewPassword_Post_ValidationError()
    {
        controller.ModelState.AddModelError("a","b");
        var result = await controller.NewPassword(new PasswordResetTokenId(Guid.Parse("cc2deed3-94c3-4cf8-91ca-f2419f8c5bd2")), new NewPasswordViewModel() {NewPassword = "", NewPasswordAgain = ""});
        Assert.IsType<ViewResult>(result);
        var view = (result as ViewResult)!;
        Assert.Null(view.ViewName);
        Assert.IsType<NewPasswordViewModel>(view.Model);
    }

    [Fact]
    public async Task NewPassword_Post_Success()
    {
        var result = await controller.NewPassword(new PasswordResetTokenId(Guid.Parse("cc2deed3-94c3-4cf8-91ca-f2419f8c5bd2")), new NewPasswordViewModel() { NewPassword = "Boronkay01", NewPasswordAgain = "Boronkay01" });
        Assert.IsType<ViewResult>(result);
        var view = (result as ViewResult)!;
        Assert.Equal("NewPasswordSuccess", view.ViewName);
    }

    [Fact]
    public async Task ConfirmEmail_Success()
    {
        var result = await controller.ConfirmEmail(new EmailConfirmationTokenId(Guid.Parse("5f20ebf5-98c7-493a-91b8-6171f683c9bc")));
        Assert.IsType<ViewResult>(result);
        var view = (result as ViewResult)!;
        Assert.Equal("EmailConfirmationSuccess", view.ViewName);
    }

    [Fact]
    public async Task ConfirmEmail_Fail()
    {
        var result = await controller.ConfirmEmail(new EmailConfirmationTokenId(Guid.Empty));
        Assert.IsType<ViewResult>(result);
        var view = (result as ViewResult)!;
        Assert.Equal("EmailConfirmationFailed", view.ViewName);
    }

    
}