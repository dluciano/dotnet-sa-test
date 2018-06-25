using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CrossSolar.Controllers;
using CrossSolar.Domain;
using CrossSolar.Models;
using CrossSolar.Repository;
using Microsoft.AspNetCore.Mvc;
using MockQueryable.Moq;
using Moq;
using Xunit;

namespace CrossSolar.Tests.Controller
{
    public class AnalyticsControllerTests
    {
        private string panelId = "0123456789ABCDEF";
        private OneHourElectricityModel testAnalyticModel;

        public AnalyticsControllerTests()
        {
            //Setup Panel Repository            
            var panels = new List<Panel>()
            {
                new Panel(){
                    Id= 1,
                    Brand = "Areva",
                    Latitude = 12.345678,
                    Longitude = 98.7655432,
                    Serial = panelId
                }
            };
            var panelsMock = panels.AsQueryable().BuildMock();

            _panelRepositoryMock.Setup(repo => repo.Query())
                .Returns(panelsMock.Object);

            //Setup Analytics Repository
            var testAnalytic = new OneHourElectricity()
            {
                Id = 1,
                PanelId = this.panelId,
                KiloWatt = 454673,
                DateTime = DateTime.Now
            };
            testAnalyticModel = new OneHourElectricityModel()
            {
                Id = testAnalytic.Id,
                DateTime = testAnalytic.DateTime,
                KiloWatt = testAnalytic.KiloWatt
            };

            var analytics = new List<OneHourElectricity>() {
                testAnalytic
            };
            var anMock = analytics.AsQueryable().BuildMock();
            _analyticsRepositoryMock.Setup(repo => repo.Query())
                .Returns(anMock.Object);

            _anaController = new AnalyticsController(_analyticsRepositoryMock.Object, _panelRepositoryMock.Object);
            _panelController = new PanelController(_panelRepositoryMock.Object);
        }

        private readonly AnalyticsController _anaController;
        private readonly PanelController _panelController;
        private readonly Mock<IPanelRepository> _panelRepositoryMock = new Mock<IPanelRepository>();
        private readonly Mock<IAnalyticsRepository> _analyticsRepositoryMock = new Mock<IAnalyticsRepository>();
            
        [Fact]
        public async Task Get_ShouldRetrievePanel()
        {
            var result = await _anaController.Get(panelId);
            var contentResult = Assert.IsType<OkObjectResult>(result);
            var model = Assert.IsType<OneHourElectricityListModel>(contentResult.Value);
            Assert.NotEmpty(model.OneHourElectricitys);
            Assert.Single(model.OneHourElectricitys);
            var m = model.OneHourElectricitys.FirstOrDefault();
            Assert.Equal(testAnalyticModel.Id, m.Id);
            Assert.Equal(testAnalyticModel.KiloWatt, m.KiloWatt);
            Assert.Equal(testAnalyticModel.DateTime, m.DateTime);
        }
    }
}