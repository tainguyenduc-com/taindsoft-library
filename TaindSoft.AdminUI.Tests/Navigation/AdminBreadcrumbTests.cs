using System.Collections.Generic;
using FluentAssertions;
using TaindSoft.AdminUI.Components.Navigation;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using Moq;
using Xunit;

namespace TaindSoft.AdminUI.Tests.Navigation;

public class AdminBreadcrumbTests
{
    private readonly Mock<IBreadcrumbLabelProvider> _labelProviderMock;
    private readonly AdminBreadcrumb _component;

    public AdminBreadcrumbTests()
    {
        // NavigationManager.BaseUri/Uri are non-virtual so Moq can't mock them.
        // Use a concrete subclass that sets base+uri via Initialize().
        var nav = new TestNavigationManager("http://localhost/", "http://localhost/admin/customers");

        _labelProviderMock = new Mock<IBreadcrumbLabelProvider>();
        _labelProviderMock.Setup(p => p.GetLabel("/admin")).Returns("Dashboard");
        _labelProviderMock.Setup(p => p.GetLabel("/admin/customers")).Returns("Customers");

        _component = new AdminBreadcrumb
        {
            Navigation = nav,
            LabelProvider = _labelProviderMock.Object,
            Segments = []
        };
        // Simulate OnInitialized
        _component.GetType().GetMethod("OnInitialized", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!
            .Invoke(_component, null);
    }

    [Fact]
    public void BuildSegments_FromUri_CreatesCorrectOrder()
    {
        // Private field _segments is populated, expose via reflection for test
        var segField = typeof(AdminBreadcrumb).GetField("_segments", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!;
        var segments = segField.GetValue(_component) as List<BreadcrumbSegment>;
        segments.Should().NotBeNull();
        segments!.Count.Should().Be(2);
        segments[0].Label.Should().Be("Dashboard");
        segments[0].Href.Should().Be("/admin");
        segments[1].Label.Should().Be("Customers");
        segments[1].Href.Should().Be("/admin/customers");
    }

    /// <summary>
    /// Minimal NavigationManager that sets base+uri via the protected Initialize method.
    /// </summary>
    private sealed class TestNavigationManager : NavigationManager
    {
        public TestNavigationManager(string baseUri, string uri)
        {
            Initialize(baseUri, uri);
        }

        protected override void NavigateToCore(string uri, NavigationOptions options)
        {
            // Not used in this test
        }
    }
}
