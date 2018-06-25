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

            _analyticsRepositoryMock.Setup(repo => repo.InsertAsync(It.IsAny<OneHourElectricity>()))
                .Returns((OneHourElectricity t) =>
                {
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

        [Fact]
        public async Task DayResults_ShouldRetrieveSumMixMaxAvg()
        {
            //Setup Analytics Repository
            var day1 = DateTime.Now;
            var day2 = day1.AddDays(1);

            var e1_100KW_day1_hour1 = new OneHourElectricity()
            {
                Id = 1,
                KiloWatt = 100,
                DateTime = day1,
                PanelId = this.panelId,
            };

            var e1_150KW_day1_hour2 = new OneHourElectricity()
            {
                Id = 2,
                KiloWatt = 150,
                DateTime = day1.AddHours(1),
                PanelId = this.panelId,
            };

            var e1_200KW_day2_hour1 = new OneHourElectricity()
            {
                Id = 3,
                KiloWatt = 200,
                DateTime = day2,
                PanelId = this.panelId,
            };

            var e1_250KW_day2_hour2 = new OneHourElectricity()
            {
                Id = 4,
                KiloWatt = 250,
                DateTime = day2.AddHours(1),
                PanelId = this.panelId,
            };

            var analytics = new List<OneHourElectricity>() {
                e1_100KW_day1_hour1, e1_150KW_day1_hour2, e1_200KW_day2_hour1, e1_250KW_day2_hour2
            };

            _analyticsRepositoryMock.Setup(repo => repo.Query())
                .Returns(analytics.AsQueryable().BuildMock().Object);

            _anaController = new AnalyticsController(_analyticsRepositoryMock.Object, _panelRepositoryMock.Object);

            //Act
            var result = await _anaController.DayResults(panelId);

            //Asserts
            var okResult = Assert.IsType<OkObjectResult>(result);
            var resultModel = Assert.IsType<List<OneDayElectricityModel>>(okResult.Value);
            Assert.Equal(2, resultModel.Count);

            var firstDay = resultModel.FirstOrDefault();//Check for the first day values
            Assert.NotNull(firstDay);

            Assert.Equal(day1.Date, firstDay.DateTime.Date);
            Assert.Equal(e1_100KW_day1_hour1.KiloWatt + e1_150KW_day1_hour2.KiloWatt, firstDay.Sum);
            Assert.Equal((e1_100KW_day1_hour1.KiloWatt + e1_150KW_day1_hour2.KiloWatt) / 2, firstDay.Average);
            Assert.Equal(e1_150KW_day1_hour2.KiloWatt, firstDay.Maximum);
            Assert.Equal(e1_100KW_day1_hour1.KiloWatt, firstDay.Minimum);
        }

        [Fact]
        public async Task Post_ShouldSaveToRegisteredPanel()
        {
            //Setup Analytics Repository
            var invalidId = "1234576890qpowke";
            var createmodel = new OneHourElectricityModel()
            {
                Id = 1,
                KiloWatt = 2000,
                DateTime = DateTime.Now
            };

            _analyticsRepositoryMock.Setup(repo => repo.InsertAsync(It.IsAny<OneHourElectricity>()))
                .Returns(Task.FromResult(1));

            _anaController = new AnalyticsController(_analyticsRepositoryMock.Object, _panelRepositoryMock.Object);

            //Act
            var result = await _anaController.Post(invalidId, createmodel);

            //Asserts
            var createResult = Assert.IsType<NotFoundResult>(result);
        }
    }
}