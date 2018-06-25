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

        private readonly PanelController _panelController;
        private readonly Mock<IPanelRepository> _panelRepositoryMock = new Mock<IPanelRepository>();
        private Mock<IAnalyticsRepository> _analyticsRepositoryMock = new Mock<IAnalyticsRepository>();
        private AnalyticsController _anaController;

        public AnalyticsControllerTests()
        {
            //Setup Panel Repository   
            var panelsMock = new List<Panel>()
            {
                new Panel(){
                    Id= 1,
                    Brand = "Areva",
                    Latitude = 12.345678,
                    Longitude = 98.7655432,
                    Serial = panelId
                }
            }.AsQueryable().BuildMock();
            _panelRepositoryMock.Setup(repo => repo.Query())
                .Returns(panelsMock.Object);
            _panelController = new PanelController(_panelRepositoryMock.Object);
        }

        [Fact]
        public async Task Get_ShouldRetrieveAnalytics()
        {
            //Setup Analytics Repository
            var testAnalytic = new OneHourElectricity()
            {
                Id = 1,
                PanelId = this.panelId,
                KiloWatt = 454673,
                DateTime = DateTime.Now
            };
            OneHourElectricityModel testAnalyticModel = new OneHourElectricityModel()
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

            //Act
            var result = await _anaController.Get(panelId);

            //Assert
            var contentResult = Assert.IsType<OkObjectResult>(result);
            var model = Assert.IsType<OneHourElectricityListModel>(contentResult.Value);
            Assert.NotEmpty(model.OneHourElectricitys);
            Assert.Single(model.OneHourElectricitys);
            var m = model.OneHourElectricitys.FirstOrDefault();
            Assert.Equal(testAnalyticModel.Id, m.Id);
            Assert.Equal(testAnalyticModel.KiloWatt, m.KiloWatt);
            Assert.Equal(testAnalyticModel.DateTime, m.DateTime);
        }

        [Fact]
        public async Task Post_ShouldSaveSave()
        {
            //Setup Analytics Repository
            var createmodel = new OneHourElectricityModel()
            {
                Id = 1,
                KiloWatt = 2000,
                DateTime = DateTime.Now
            };            

            Func<OneHourElectricity, OneHourElectricity> ex = (t) => { return new OneHourElectricity();  };

            _analyticsRepositoryMock.Setup(repo => repo.InsertAsync(It.IsAny<OneHourElectricity>()))
                .Returns((OneHourElectricity t) => {
                    t.Id = createmodel.Id;
                    t.DateTime = createmodel.DateTime;
                    t.KiloWatt = createmodel.KiloWatt;
                    t.PanelId = panelId;
                    return Task.FromResult(1);
                });

            _anaController = new AnalyticsController(_analyticsRepositoryMock.Object, _panelRepositoryMock.Object);

            //Act
            var result = await _anaController.Post(panelId, createmodel);

            //Asserts
            var createResult = Assert.IsType<CreatedResult>(result);
            Assert.Equal(201, createResult.StatusCode);
            var resultModel = Assert.IsType<OneHourElectricityModel>(createResult.Value);
            Assert.Equal(createmodel.Id, resultModel.Id);
            Assert.Equal(createmodel.KiloWatt, resultModel.KiloWatt);
            Assert.Equal(createmodel.DateTime, resultModel.DateTime);            
        }
    }
}