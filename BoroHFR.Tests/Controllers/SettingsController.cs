using System.Security.Claims;
using BoroHFR.Data;
using BoroHFR.Models;
using BoroHFR.Services;
using BoroHFR.ViewModels.Settings;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Razor;
using Microsoft.AspNetCore.Mvc.Routing;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Moq;
using Moq.EntityFrameworkCore;
using NuGet.Frameworks;

namespace BoroHFR.Tests.Controllers;

public class SettingsController
{
    private readonly BoroHFR.Controllers.SettingsController controller;

    public SettingsController()
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
        context.Setup(x => x.User).Returns(new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("Id", "22010625-2548-4282-9fb2-a3bb4a77d2ba") })));

        controller = new BoroHFR.Controllers.SettingsController(dbContextMock.Object);
        controller.ControllerContext = new ControllerContext() { HttpContext = context.Object };
    }

    [Fact]
    public void Index_RedirectToUserData()
    {
        var result = controller.Index();
        Assert.IsType<RedirectToActionResult>(result);
        var red = (result as RedirectToActionResult)!;
        Assert.Equal("UserData", red.ActionName);
    }

    [Fact]
    public async Task UserData_ReturnsView()
    {
        var result = await controller.UserData();
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public void ChangePassword_ReturnsView()
    {
        var result = controller.ChangePassword();
        Assert.IsType<ViewResult>(result);
    }

    [Fact]
    public async Task ChangePassword_Success()
    {
        var result = await controller.ChangePassword(new ChangePasswordViewModel()
        {
            OldPassword = "apitest1234", NewPassword = "Boronkay02", NewPasswordAgain = "Boronkay02"
        });
        Assert.IsType<ViewResult>(result);
        var view = (result as ViewResult)!;
        var model = view.Model as ChangePasswordViewModel;
        Assert.NotNull(model);
        Assert.True(model.Success);
    }

    [Fact]
    public async Task ChangePassword_ValidationError()
    {
        controller.ModelState.AddModelError("a", "b");
        var result = await controller.ChangePassword(new ChangePasswordViewModel());
        Assert.IsType<ViewResult>(result);
        var view = (result as ViewResult)!;
        var model = view.Model as ChangePasswordViewModel;
        Assert.NotNull(model);
        Assert.False(model.Success);
    }

    [Fact]
    public async Task ChangePassword_BadPassword()
    {
        var result = await controller.ChangePassword(new ChangePasswordViewModel()
        {
            OldPassword = "Boronkay123",
            NewPassword = "Boronkay02",
            NewPasswordAgain = "Boronkay02"
        });
        Assert.IsType<ViewResult>(result);
        var view = (result as ViewResult)!;
        var model = view.Model as ChangePasswordViewModel;
        Assert.NotNull(model);
        Assert.False(model.Success);
        Assert.Equal("Hibás a jelenlegi jelszó.", model.ErrorMessage);
    }

    [Fact]
    public async Task GroupSettings_ReturnsView()
    {
        var result = await controller.GroupSettings();
        Assert.IsType<ViewResult>(result);
        var view = (result as ViewResult)!;
        var model = view.Model as GroupSettingsViewModel;
        Assert.NotNull(model);
        Assert.NotNull(model.GroupMemberships);
    }


    //TODO get some results back
    [Fact]
    public async Task SubjectSearch_ReturnsPartial()
    {
        var result = await controller.SubjectSearch(new BoroHFR.Controllers.SettingsController.SubjectSearchData("tárgy"));
        Assert.IsType<PartialViewResult>(result);
        var view = (result as PartialViewResult)!;
        var model = view.Model as SubjectSearchResultViewModel;
        Assert.NotNull(model);
        Assert.NotNull(model.Subjects);
    }

    [Fact]
    public async Task JoinedGroups_ReturnsPartial()
    {
        var result = await controller.JoinedGroups();
        Assert.IsType<PartialViewResult>(result);
        var view = (result as PartialViewResult)!;
        var model = view.Model as GroupSettingsViewModel;
        Assert.NotNull(model);
        Assert.NotNull(model.GroupMemberships);
    }

    [Fact]
    public async Task JoinGroup_Success()
    {
        var result = await controller.JoinGroup(new GroupId(Guid.Parse("1b440071-93fc-4340-8c8e-878caa4cb29f")));
        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task JoinGroup_WrongClass()
    {
        var result = await controller.JoinGroup(new GroupId(Guid.Parse("b245e66f-24b0-4228-9fc1-7d25b4c6ada6")));
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task JoinGroup_DoesntExist()
    {
        var result = await controller.JoinGroup(new GroupId());
        Assert.IsType<BadRequestObjectResult>(result);
    }

    [Fact]
    public async Task LeaveGroup_Success()
    {
        var result = await controller.LeaveGroup(new GroupId(Guid.Parse("19bae99b-79df-4c91-b010-472b214aa956")));
        Assert.IsType<OkResult>(result);
    }

    [Fact]
    public async Task LeaveGroup_DoesntExist()
    {
        var result = await controller.LeaveGroup(new GroupId());
        Assert.IsType<BadRequestResult>(result);
    }
}