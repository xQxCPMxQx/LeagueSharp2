using EasyCaching.Core;
using Microservice.Common.Enums;
using Microservice.Common.HttpClient;
using Microservice.Common.HttpClient.Enums;
using Microservice.Common.HttpClient.Models;
using Microservice.Common.Models;
using Microservice.Infrastructure.Mongo;
using Microservice.ProductOffering.API.Constants;
using Microservice.ProductOffering.API.DecideClasses;
using Microservice.ProductOffering.API.DecoratorClasses;
using Microservice.ProductOffering.API.Helper;
using Microservice.ProductOffering.API.Models;
using Microservice.ProductOffering.API.Models.RequestModels;
using Microservice.ProductOffering.API.Models.ResponseModels;
using Microservice.ProductOffering.API.PriceCalculatorClasses;
using Microservice.ProductOffering.API.Services;
using Microservice.ProductOffering.Infrastructure.Entities;
using Microservice.ProductOffering.Infrastructure.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MongoDB.Driver.Linq;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Microservice.ProductOffering.API.Controllers
{
    [Authorize(AuthorizeConstants.Read)]
    public class ProductOfferController : BaseController
    {
        private readonly OfferTypeConstService _offerTypeConstService;
        private readonly IServiceProvider _provider;
        private readonly IMongoRepository _mongoRepository;
        private readonly IEasyCachingProviderFactory _cacheProvider;
        private readonly ICachingHelper _cachingHelper;
        private readonly IHttpClientHelper _httpClient;
        private readonly AppSettings _appSettings;
        private readonly ILogger _logger;
        public ProductOfferController(ILogger<ProductOfferController> logger, IOptions<AppSettings> options, OfferTypeConstService offerTypeConstService, ICachingHelper cachingHelper, IServiceProvider provider, IMongoRepository mongoRepository, IEasyCachingProviderFactory cacheProvider, IHttpClientHelper httpClient /*, ILogger<ProductOfferController> logger*/)
        {
            _appSettings = options.Value;
            _offerTypeConstService = offerTypeConstService;
            _provider = provider;
            _mongoRepository = mongoRepository;
            _cacheProvider = cacheProvider;
            _cachingHelper = cachingHelper;
            _httpClient = httpClient;
            _logger = logger;
        }

        [AllowAnonymous]
        [HttpGet("GetToken")]
        public async Task<IActionResult> GetToken()
        {
            var tokenModel = new TokenRequestModel
            {
                TokenType = ClientHelperTokenTypes.PasswordCredential,
                SsoEndpoint = _appSettings.SsoClient.SsoEndpoint,

                ClientId = "MicroServiceUi_BackEndLoginClient",
                ClientSecret = "Dsmart",

                Username = "API.USER@DSMART.COM.TR",
                Password = "Aa123-zx1",

                Scopes = "productofferingapi.read",
                Parameters = new Dictionary<string, string>()
            };
            return Ok(await _httpClient.GetToken(tokenModel));
        }
        [AllowAnonymous]
        [HttpGet("GetUIToken")]
        public async Task<IActionResult> GetUIToken()
        {
            TokenRequestModel m_AuthTokenModel = new TokenRequestModel();

            var auth = _httpClient.GetAsync<Dictionary<string, object>>("https://crmmicroservicecustomerapi.dssys.int/", "GetToken").Result;
            object token = "";
            auth.TryGetValue("accessToken", out token);

            m_AuthTokenModel.TokenType = ClientHelperTokenTypes.Delegation;
            m_AuthTokenModel.SsoEndpoint = "https://crmsso.dssys.int/";
            m_AuthTokenModel.ClientId = "MicroServiceUi_BackEndLoginClient";
            m_AuthTokenModel.ClientSecret = "Dsmart";
            m_AuthTokenModel.Scopes = "productofferingapi.read resourceapi.read workorderapi.write entitlementapi.read businessinteractionapi.write billingapi.admin customerapi.read dealerapi.read identityserverapi.admin";
            // tokenModel.AccessToken = User.FindFirst("access_token").Value;
            m_AuthTokenModel.AccessToken = token.ToString();
            return Ok(m_AuthTokenModel);
        }

        [HttpGet(ProductOfferingApiMethodConst.ProductOffer.ResetCache)]
        [ProducesResponseType(200, Type = typeof(ApiResponse))]
        public async Task<IActionResult> ResetCache()
        {
            try
            {
                await _cachingHelper.ResetCacheOfferSearch();
                return Ok(new ApiResponse(true));
            }
            catch (Exception ex)
            {
                return Ok(new ApiResponse(false) { Message = ex.Message });
            }
        }



        [HttpGet(ProductOfferingApiMethodConst.ProductOffer.GetOffer + "{offerId}")]
        [ProducesResponseType(200, Type = typeof(ApiSearchResponse<ProductOfferResponseModel>))]
        [AllowAnonymous]
        public async Task<IActionResult> GetOffer(Guid offerId)
        {
            var provider = _cacheProvider.GetCachingProvider("DefaultInMemory");
            if (!await provider.ExistsAsync("ProductOfferProductMapCache"))
            {
                //set cache
                await _cachingHelper.ResetCacheOfferSearch();
            }
            var productOfferDic = (await provider.GetAsync<Dictionary<Guid, ProductOffer>>("ProductOfferCache")).Value.Values;

            var offer = productOfferDic.FirstOrDefault(x => x.ProductOfferId == offerId);
            if (offer == null)
            {
                return Ok(new ApiSearchResponse<ProductOfferResponseModel>
                {
                    IsSuccess = false,
                });
            }
            var response = new ApiSearchResponse<ProductOfferResponseModel>
            {
                IsSuccess = true,
            };
            var offerList = new List<ProductOfferResponseModel>();
            offerList.Add(new ProductOfferResponseModel
            {
                ProductOffer = new Common.Models.IdNameModel<Guid> { Id = offer.ProductOfferId, Name = offer.Name },
                Term = new Common.Models.DurationModel
                {
                    Duration = offer.Term.Duration,
                    DurationType = offer.Term.DurationType,
                },
                OfferRoutingType = offer.OfferRoutingType,
            });
            response.Data.Items = offerList;
            response.Data.PageSize = offerList?.Count ?? 0;
            response.Data.TotalCount = offerList?.Count ?? 0;
            return Ok(response);

        }

        [HttpPost(ProductOfferingApiMethodConst.ProductOffer.GetOfferById)]
        [ProducesResponseType(200, Type = typeof(ApiSearchResponse<ProductOfferResponseModel>))]
        [AllowAnonymous]
        public async Task<IActionResult> GetOfferById([FromBody] ProductOfferByIdRequestModel request)
        {

            try
            {
                //_logger.LogInformation(Newtonsoft.Json.JsonConvert.SerializeObject(request));

                var provider = _cacheProvider.GetCachingProvider("DefaultInMemory");
                if (!await provider.ExistsAsync("ProductOfferProductMapCache"))
                {
                    //set cache
                    await _cachingHelper.ResetCacheOfferSearch();
                }
                var productOfferProductMapDic = (await provider.GetAsync<Dictionary<Guid, ProductOfferProductMap>>("ProductOfferProductMapCache")).Value.Values;
                var productDic = (await provider.GetAsync<Dictionary<Guid, Product>>("ProductCache")).Value.Values;
                var productOfferDic = (await provider.GetAsync<Dictionary<Guid, ProductOffer>>("ProductOfferCache")).Value.Values;
                var productOfferCatalogDic = (await provider.GetAsync<Dictionary<Guid, ProductOfferCatalog>>("ProductOfferCatalogCache")).Value.Values;

                var productCompositeDic = (await provider.GetAsync<Dictionary<Guid, ProductComposite>>("ProductCompositeCache")).Value.Values;
                var productComponentDic = (await provider.GetAsync<Dictionary<Guid, ProductComponent>>("ProductComponentCache")).Value.Values;
                var productOfferRuleDic = (await provider.GetAsync<Dictionary<Guid, ProductOfferRule>>("ProductOfferRuleCache")).Value;
                var productSpecificationCharacteristicDic = (await provider.GetAsync<Dictionary<Guid, ProductSpecificationCharacteristic>>("ProductSpecificationCharacteristicCache")).Value.Values;
                var requestParameterDic = (await provider.GetAsync<Dictionary<string, RequestParameter>>("RequestParameterCache")).Value.Values;
                var campaignDetailsDic = (await provider.GetAsync<Dictionary<Guid, CampaignDetails>>("CampaignDetailsCache")).Value;
                var productCompositeEquivalentDic = (await provider.GetAsync<Lookup<Guid, ProductCompositeEquivalents>>("ProductCompositeEquivalentsCache")).Value;


                var characteristicList = await CharacteristicsParametersSplitterHelper.SplitCharacteristics(request.CharacteristicList, _mongoRepository);
                var requestParemeters = await CharacteristicsParametersSplitterHelper.SplitParameters(request.CharacteristicList, _mongoRepository);

                var productSpecifications = request.ProductSpecifications;
                if (productSpecifications.Any(x=> x.Id == ProductSpecificationEnum.Internet.Id))
                {
                    productSpecifications.Add(ProductSpecificationEnum.Naked);
                }
               

                //sadece aktif productlari aktifler
                productDic = productDic.Where(x => !x.Disabled).ToDictionary(x => x.ProductId).Values;

                var versions = VersionHelper.Find(request.OfferTypeId, productSpecifications);


                var grupList = UserGroupIds;
                if (UserId == Guid.Parse("beaee219-beb9-4617-93bf-68e71d43da61"))
                {
                    grupList = new List<int> { 1, 70, 60, 84, 607, 610, 609, 85, 51, 72 };
                }
                var query = from offerMap in productOfferProductMapDic
                            join product in productDic on offerMap.ProductId equals product.ProductId
                            join offer in productOfferDic on offerMap.ProductOfferId equals offer.ProductOfferId
                            join catalog in productOfferCatalogDic on offer.ProductOfferCatalogId equals catalog.ProductOfferCatalogId
                            where offer.ProductOfferId == request.OfferId 
                              && catalog.SalesChannels.Any(x => x == request.SalesChannel)
                              && grupList.Any(x => catalog.Groups.Contains(x))
                              && offerMap.StartDate <= DateTime.Now
                              && offerMap.EndDate >= DateTime.Now
                              && product.StartDate <= DateTime.Now
                              && product.EndDate >= DateTime.Now
                              && catalog.StartDate <= DateTime.Now
                              && catalog.EndDate >= DateTime.Now
                              && offer.StartDate <= DateTime.Now
                              && offer.EndDate >= DateTime.Now
                              && offer.OfferType.Id == request.OfferTypeId
                            //&& offer.OfferType.Id == OfferType.ReverseCrossFromInternetToTv.Id
                            //&& offerMap.ProductOfferId == Guid.Parse("274cf35e-0e71-4f72-8dc7-9e6564172cf0")
                            //&& offerMap.ProductId == Guid.Parse("f4ace640-a9e7-461f-af2a-45c7bc3c9535")
                            //&& new List<Guid> { Guid.Parse("a7791360-b819-40be-8fdc-5c36b157cade"), Guid.Parse("d8cb29ab-bd99-458a-b772-d187f2ae4ead"), Guid.Parse("0406397c-e88c-4d0d-b496-a32ee0bfb43e") }.Contains( offer.ProductOfferCatalogId)
                            //&& offerMap.ProductComposites.Any(x=>x.ProductCompositeId == Guid.Parse("09ce76fc-b3c8-48c5-b69d-9167fe8b7797"))
                            select new
                            {
                                offerMap.ProductOfferProductMapId,
                                offerMap.Version,
                                offerMap.Rules,
                                Product = new Common.Models.IdNameModel<Guid> { Id = product.ProductId, Name = product.Name },
                                ProductOffer = new Common.Models.IdNameModel<Guid> { Id = offer.ProductOfferId, Name = offer.Name },
                                ProductOfferCatalog = new Common.Models.IdNameModel<Guid> { Id = catalog.ProductOfferCatalogId, Name = catalog.Name },
                                OfferRoutingType = offer.OfferRoutingType,
                                Term = new Common.Models.DurationModel
                                {
                                    Duration = offer.Term.Duration,
                                    DurationType = offer.Term.DurationType,
                                },
                                OTFs = offer.OTFs,
                                ProductComposites = offerMap.ProductComposites,
                                ProductOfferRules = offer.ProductOfferRules,
                                offer.PaymentType,
                                offer.FeeType,
                                offer.InstallmentsCount,
                                offer.StbOwnerOnly,
                                offerMap.BonusProductComposites,
                                CampaignDetailId = offer.CampaignDetailId ?? Guid.Empty,
                                offer.PriceCalculationClass,
                                offer.OfferType,
                                offer.ProductOfferDecorators
                            };

                if (!string.IsNullOrEmpty(request.CouponCode))
                {
                    var coupon = await _mongoRepository.GetAsync<Coupons>(x => x.Code == request.CouponCode);
                    if (coupon.Count > coupon.Used && coupon.EndDate.Date <= DateTime.Now.Date && coupon.StartDate.Date >= DateTime.Now.Date)
                    {
                        if (coupon.ProductOfferProductMapId.HasValue)
                        {
                            query = query.Where(x => x.ProductOfferProductMapId == coupon.ProductOfferProductMapId);
                        }
                        else if (coupon.ProductOfferId.HasValue)
                        {
                            query = query.Where(x => x.ProductOffer.Id == coupon.ProductOfferId);
                        }
                        else if (coupon.ProductOfferCatalogId.HasValue)
                        {
                            query = query.Where(x => x.ProductOfferCatalog.Id == coupon.ProductOfferCatalogId);
                        }
                        else
                        {
                            //kupon kodu hatalı. response boş olmalı.
                            return Ok(new ApiResponse("No any offers to depend this Coupon Code!"));
                        }
                    }
                    else
                    {
                        return Ok(new ApiResponse("Invalid Coupon Code!"));
                    }
                }

                var searchResult = query.ToList();
                //searchResult = searchResult.Where(x => x.ProductOffer.Name.Contains("2301004") && x.Product.Name.ToLower().Contains("fiber vdsl 24mb ekadar (fttb) - limitsiz") && x.Product.Name.ToLower().Contains("yalin")).ToList();
               

                var responseData = new ConcurrentBag<ProductOfferResponseModel>();
                if (searchResult != null && searchResult.Any())
                {
                    var productCompositeIds = searchResult.SelectMany(x => x.ProductComposites).Select(x => x.ProductCompositeId).Distinct().ToList();

                    var productCompositeList = productCompositeDic.Where(x => productCompositeIds.Contains(x.ProductCompositeId))
                                                .Select(composite => new
                                                {
                                                    ProductComposite = new Common.Models.IdNameModel<Guid> { Id = composite.ProductCompositeId, Name = composite.Name },
                                                    composite.ProductSpecification,
                                                    composite.ProductComponents,
                                                    composite.BillingProductComposite,
                                                    composite.ResourceCompositeBundles,
                                                    composite.ProductCompositePrices,
                                                    composite.Neighborhoods
                                                }).ToDictionary(x => x.ProductComposite.Id);


                    var productComponentIds = productCompositeList.SelectMany(x => x.Value.ProductComponents).Distinct().ToList();
                    var productComponentList = productComponentDic.Where(x => productComponentIds.Contains(x.ProductComponentId)).ToList();


                    var productSpecificationCharacteristicValueIds = productComponentList.SelectMany(x => x.ProductSpecificationCharacteristicValues).Distinct().ToList();
                    var productSpecificationCharacteristicList = productSpecificationCharacteristicDic.Where(x => x.ProductSpecificationCharacteristicValues.Any(t => productSpecificationCharacteristicValueIds.Contains(t.ProductSpecificationCharacteristicValueId))).ToList();

                    if (characteristicList != null && characteristicList.Any())
                    {
                        var filteredCompositeIdList = new List<Guid>();
                        foreach (var characteristics in characteristicList)
                        {
                            var filteredCharacteristicIdList = characteristics.SelectMany(x =>
                                        productSpecificationCharacteristicList
                                                .First(t => t.Name == x.Property)
                                                .ProductSpecificationCharacteristicValues
                                                .Where(z => z.Value == x.Value)?
                                                .Select(sa => new { x.Property, sa.ProductSpecificationCharacteristicValueId }))
                                       .ToList();

                            filteredCompositeIdList.AddRange(
                                productCompositeList.Where(x =>
                                    filteredCharacteristicIdList.GroupBy(k => k.Property).All(kk =>
                                             x.Value.ProductComponents.Select(xa => productComponentList.First(xat => xat.ProductComponentId == xa))
                                                        .SelectMany(xt => xt.ProductSpecificationCharacteristicValues)
                                                        .Any(t => kk.Any(saa => saa.ProductSpecificationCharacteristicValueId == t))))
                                .Select(x => x.Value.ProductComposite.Id).ToList());
                            var aa = productCompositeList.Where(x =>
                                     filteredCharacteristicIdList.GroupBy(k => k.Property).All(kk =>
                                              x.Value.ProductComponents.Select(xa => productComponentList.First(xat => xat.ProductComponentId == xa))
                                                         .SelectMany(xt => xt.ProductSpecificationCharacteristicValues)
                                                         .Any(t => kk.Any(saa => saa.ProductSpecificationCharacteristicValueId == t))))
                                .Select(x => x.Value.ProductComposite.Id).ToList();
                        }
                        searchResult = searchResult
                            .Where(x => x.ProductComposites
                                        .Any(t => filteredCompositeIdList.Contains(t.ProductCompositeId)))
                            .ToList();
                    }


                    var dynamicExpressoParameters = CharacteristicsParametersSplitterHelper.SplitDynamicExpressoParametersFromRequestModel(request.CharacteristicList);
                   

                    if (requestParemeters != null && requestParemeters.Any())
                    {
                        var properties = requestParemeters.Select(a => a.Property);
                        var requestParameters = requestParameterDic.Where(x => properties.Contains(x.Name)).ToList();
                        if (requestParameters != null && requestParameters.Any())
                        {
                            foreach (var requestParameter in requestParameters)
                            {
                                var decideClass1 = ActivatorUtilities.CreateInstance(_provider, Type.GetType(requestParameter.CalculationClass)) as IGetParameters;
                                //foreach (var version in versions)
                                //{
                                //    dynamicExpressoParameters = (await decideClass1.Calculate(dynamicExpressoParameters, requestParameter.RuleParameters, request.OfferTypeId, version, requestParemeters.First(df => df.Property == requestParameter.Name).Value));
                                //}
                            }
                        }
                    }

                    //#region Taahütsüz upgrade teklifleri

                    ////var defaultRequestParameterRegistered = dynamicExpressoParameters.FirstOrDefault(x => x.Name == "IsUnRegistered");

                    ////if (request.OfferTypeId == OfferType.Upgrade.Id && defaultRequestParameterRegistered?.Value == "True")
                    ////{
                    ////   searchResult = searchResult.Where(x=>x.ProductOffer.Name.Contains("Taahhütsüz") && x.Term.Duration==0).ToList();
                    ////}

                    //#endregion

                    var defaultRequestParameter = requestParameterDic.First(x => x.Name == "Default");
                    var decideClass = ActivatorUtilities.CreateInstance(_provider, Type.GetType(defaultRequestParameter.CalculationClass)) as IGetParameters;

                    //do not show same product when upgrade offer
                    if (_offerTypeConstService.OfferTypes.Select(xa => xa.Id).Contains(OfferType.Upgrade.Id))
                    {
                        searchResult = searchResult.Where(x => x.Product.Id != _offerTypeConstService.ProductId)?.ToList();
                    }

                    //must show same productComposite with cross offers when current product contains paytv 
                    if (
                            (
                                _offerTypeConstService.OfferTypes.Select(xa => xa.Id).Contains(OfferType.CrossFromTvToInternet.Id) ||
                                _offerTypeConstService.OfferTypes.Select(xa => xa.Id).Contains(OfferType.TransferInternet.Id)
                            ) &&
                            _offerTypeConstService.CurrentProductSpecifications.Any(aa => aa.Id == ProductSpecificationEnum.PayTv.Id))
                    {
                        var currentPayTvProductCompositeId = _offerTypeConstService.CurrentCompositePriceList.First(x => x.ProductSpecification.Id == ProductSpecificationEnum.PayTv.Id).ProductCompositeId;

                        //eslenigi varsa onu al
                        currentPayTvProductCompositeId = productCompositeEquivalentDic[currentPayTvProductCompositeId]?.SingleOrDefault(x => versions.Contains(x.Version))?.NewProductCompositeId ?? currentPayTvProductCompositeId;

                        searchResult = searchResult.Where(x => x.ProductComposites.Any(aa => aa.ProductCompositeId == currentPayTvProductCompositeId))?.ToList();
                    }

                    //must show same productComposite with raise offers 
                    if (_offerTypeConstService.OfferTypes.Select(xa => xa.Id).Contains(OfferType.Raise.Id))
                    {
                        //Naked için filtre iptal edildi.
                        foreach (var item in _offerTypeConstService.CurrentCompositePriceList.Where(x => x.ProductSpecification.Id != 5 && x.ProductSpecification.Id != ProductSpecificationEnum.Go.Id).ToList())
                        {
                            item.ProductCompositeId = productCompositeEquivalentDic[item.ProductCompositeId]?.SingleOrDefault(x => versions.Contains(x.Version))?.NewProductCompositeId ?? item.ProductCompositeId;

                            searchResult = searchResult.Where(x => x.ProductComposites.Any(aa => aa.ProductCompositeId == item.ProductCompositeId))?.ToList();
                        }

                    }


                    //Composite black list when change offer
                    if (_offerTypeConstService.OfferTypes.Select(xa => xa.Id).Contains(OfferType.Upgrade.Id)
                        || _offerTypeConstService.OfferTypes.Select(xa => xa.Id).Contains(OfferType.CrossFromTvToInternet.Id)
                        || _offerTypeConstService.OfferTypes.Select(xa => xa.Id).Contains(OfferType.CrossFromInternetToTv.Id)
                         || _offerTypeConstService.OfferTypes.Select(xa => xa.Id).Contains(OfferType.CrossFromInternetToInternetGo.Id)
                        || _offerTypeConstService.OfferTypes.Select(xa => xa.Id).Contains(OfferType.ReverseCrossFromTvToInternet.Id)
                        || _offerTypeConstService.OfferTypes.Select(xa => xa.Id).Contains(OfferType.ReverseCrossFromInternetToTv.Id))
                    {
                        //eslenigi varsa onu al
                        var currentCompositeIds = _offerTypeConstService.CurrentCompositePriceList?.Select(x => productCompositeEquivalentDic[x.ProductCompositeId]?.SingleOrDefault(x => versions.Contains(x.Version))?.NewProductCompositeId ?? x.ProductCompositeId)?.ToList();

                        if (currentCompositeIds != null && currentCompositeIds.Count > 0)
                        {
                            var blackList = productCompositeDic.Where(x => currentCompositeIds.Contains(x.ProductCompositeId) && x.BlackProductCompositeListWhenChangeOffer != null && x.BlackProductCompositeListWhenChangeOffer.Any())?.SelectMany(x => x.BlackProductCompositeListWhenChangeOffer)?.ToList();

                            if (blackList != null && blackList.Count > 0)
                            {
                                searchResult = searchResult.Where(x => !x.ProductComposites.Any(t => blackList.Contains(t.ProductCompositeId)))?.ToList();
                            }
                        }
                    }
                    if (request.OfferTypeId == OfferType.CrossFromInternetToInternetGo.Id)
                    {
                        searchResult = searchResult.Where(x => x.OfferType.Id == request.OfferTypeId).ToList();

                    }
                    else
                    {
                        searchResult = searchResult.Where(x => _offerTypeConstService.OfferTypes.Select(xa => xa.Id).Contains(x.OfferType.Id)).ToList();
                    }

                    var priceClassDict = new Dictionary<string, IPriceCalculator>();

                    foreach (var className in searchResult.Where(x => x.PriceCalculationClass != null).Select(x => x.PriceCalculationClass.ClassName).Distinct())
                    {
                        var priceCalcClass = ActivatorUtilities.CreateInstance(_provider, Type.GetType(className)) as IPriceCalculator;

                        priceClassDict.Add(className, priceCalcClass);
                    }

                    var offerRuleList = searchResult.Where(x => x.ProductOfferRules != null && x.ProductOfferRules.Any()).GroupBy(x => x.ProductOffer.Id).Select(x => new { x.First().ProductOfferRules, ProductOfferId = x.Key });

                    Dictionary<Guid, bool> offerRuleResultDict = new Dictionary<Guid, bool>();

                    //offerRules
                    foreach (var entity in offerRuleList)
                    {
                        foreach (var productOfferRuleId in entity.ProductOfferRules)
                        {
                            if (!offerRuleResultDict.ContainsKey(productOfferRuleId))
                            {
                                var productOfferRule = productOfferRuleDic[productOfferRuleId];
                                if (!bool.Parse(DynamicExpressoHelper.EvaluateAsync(new DynamicExpressoModel { Script = productOfferRule.Formula, Parameters = dynamicExpressoParameters }).ToString()))
                                {
                                    searchResult = searchResult.Where(x => x.ProductOffer.Id != entity.ProductOfferId).ToList();
                                    offerRuleResultDict.Add(productOfferRuleId, false);
                                    break;
                                }
                                else
                                    offerRuleResultDict.Add(productOfferRuleId, true);
                            }
                            else
                            {
                                if (!offerRuleResultDict[productOfferRuleId])
                                {
                                    searchResult = searchResult.Where(x => x.ProductOffer.Id != entity.ProductOfferId).ToList();
                                    break;
                                }
                            }
                        }
                    }

                    var offerDecoratorList = searchResult.Where(x => x.ProductOfferDecorators != null && x.ProductOfferDecorators.Any(y => y.Version == x.Version)).GroupBy(x => x.ProductOffer.Id).Select(x => new { x.First().ProductOfferDecorators.Single(y => y.Version == x.First().Version).ClassName, ProductOfferId = x.Key }).ToDictionary(x => x.ProductOfferId);

                    var offerDecoratorClassDict = new Dictionary<Guid, IOfferDecorator>();

                    foreach (var x in offerDecoratorList)
                    {
                        var decoratorClass = ActivatorUtilities.CreateInstance(_provider, Type.GetType(x.Value.ClassName)) as IOfferDecorator;

                        offerDecoratorClassDict.Add(x.Key, decoratorClass);
                    }

                    var offerHelper = new OfferHelper(_logger, _offerTypeConstService, _provider, _cacheProvider);



                    foreach (var entity in searchResult)

                    {
                        bool isRulesSucceed = true;
                        List<DynamicExpressoParameterModel> localDynamicExpressoParameters = new List<DynamicExpressoParameterModel>();

                        foreach (var item in dynamicExpressoParameters)
                        {
                            localDynamicExpressoParameters.Add(new DynamicExpressoParameterModel
                            {
                                Name = item.Name,
                                Type = item.Type,
                                Value = item.Value
                            });
                        }
                        try
                        {
                            #region Check Offer Rules

                            var productCompositePrices = entity.ProductComposites.Select(t => productCompositeList[t.ProductCompositeId].ProductCompositePrices.Single(x => x.Version == entity.Version)).ToList();

                            localDynamicExpressoParameters = decideClass.Calculate(localDynamicExpressoParameters, defaultRequestParameter.RuleParameters, request.OfferTypeId, entity.Version, productCompositePrices).GetAwaiter().GetResult();

                            if (entity.Rules != null && entity.Rules.Any())
                            {
                                foreach (var productOfferRuleId in entity.Rules)
                                {
                                    if (isRulesSucceed)
                                    {
                                        var productOfferRule = productOfferRuleDic[productOfferRuleId];

                                        //4 e 40 tarifelerde intenet olmamasi kosulu var. Ama upgrade yaparken veya intenete paytv satarken secilebilir olmali. Tabi mevcut urunlerinde boyle bir sey varsa.
                                        if ((request.OfferTypeId == OfferType.Upgrade.Id || request.OfferTypeId == OfferType.CrossFromInternetToTv.Id) && productOfferRule.Formula.Contains("ChurnDuration") && _offerTypeConstService.CurrentMainUntouchedComposites.Any(x => entity.ProductComposites.Any(y => y.ProductCompositeId == x)))
                                        {

                                        }
                                        else
                                            isRulesSucceed = isRulesSucceed && bool.Parse(DynamicExpressoHelper.EvaluateAsync(new DynamicExpressoModel { Script = productOfferRule.Formula, Parameters = localDynamicExpressoParameters }).ToString());
                                    }
                                }
                            }



                            if (entity.BonusProductComposites != null && entity.BonusProductComposites.Any())
                            {
                                List<BonusProductComposite> removeBonusList = new List<BonusProductComposite>();

                                foreach (var bonusProductComposite in entity.BonusProductComposites)
                                {
                                    if (!string.IsNullOrEmpty(bonusProductComposite.Rule) && !bool.Parse(DynamicExpressoHelper.EvaluateAsync(new DynamicExpressoModel { Script = bonusProductComposite.Rule, Parameters = localDynamicExpressoParameters }).ToString()))
                                        removeBonusList.Add(bonusProductComposite);
                                }

                                foreach (var item in removeBonusList)
                                {
                                    entity.BonusProductComposites.Remove(item);
                                }
                            }

                            #endregion

                            //check  Neighborhood
                            if (request.NeighborhoodCode.HasValue && request.NeighborhoodCode > 0)
                            {
                                foreach (var item in entity.ProductComposites)
                                {
                                    if (productCompositeList[item.ProductCompositeId].Neighborhoods != null && productCompositeList[item.ProductCompositeId].Neighborhoods.Count > 0)
                                    {
                                        if (!productCompositeList[item.ProductCompositeId].Neighborhoods.Select(xa => xa.Neighborhood.Id).Any(xa => xa == request.NeighborhoodCode.Value))
                                        {
                                            isRulesSucceed = false;
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            isRulesSucceed = false;
                        }

                        if (isRulesSucceed)
                        {
                            try
                            {
                                IPriceCalculator priceCalcClass = null;
                                if (entity.PriceCalculationClass != null)
                                {
                                    priceCalcClass = priceClassDict[entity.PriceCalculationClass.ClassName];
                                }

                                var offer = new ProductOfferResponseModel
                                {
                                    BundleProduct = entity.Product,
                                    ProductOffer = entity.ProductOffer,
                                    ProductOfferCatalog = entity.ProductOfferCatalog,
                                    OfferRoutingType = entity.OfferRoutingType,
                                    Term = entity.Term,
                                    PaymentType = entity.PaymentType,
                                    OfferType = entity.OfferType,
                                    InstallmentsCount = entity.InstallmentsCount,
                                    StbOwnerOnly = entity.StbOwnerOnly,
                                    FeeType = entity.FeeType,
                                    Version = entity.Version
                                };
                                if (entity.CampaignDetailId != Guid.Empty)
                                {
                                    var campaignDetail = campaignDetailsDic[entity.CampaignDetailId];
                                    offer.CampaignDetail = new CampaignDetailResponseModel
                                    {
                                        CampaignDetailId = campaignDetail.CampaignDetailId,
                                        Name = campaignDetail.Name,
                                        Form = campaignDetail.Form
                                    };
                                }

                                offer.OTFs = entity.OTFs
                                        ?.Select(t => new OTFModel<decimal>
                                        {
                                            Default = t.Default,
                                            OTFId = t.OTFId,
                                            OTFType = t.OTFType,
                                            Price = new PriceModel<decimal>
                                            {
                                                Price = decimal.Parse(t.Price.Price),
                                                Duration = new Common.Models.DurationModel
                                                {
                                                    Duration = t.Price.Duration.Duration,
                                                    DurationType = t.Price.Duration.DurationType,
                                                },
                                                DurationStart = new Common.Models.DurationModel
                                                {
                                                    Duration = t.Price.DurationStart.Duration,
                                                    DurationType = t.Price.DurationStart.DurationType,
                                                }
                                            }
                                        }).ToList();

                                offer.ProductOfferRules = entity.ProductOfferRules
                                        ?.Select(t => new Common.Models.IdNameModel<Guid>
                                        {
                                            Id = t,
                                            Name = productOfferRuleDic[t].Formula
                                        }).ToList();

                                offer.ProductComposites = new List<ProductCompositeResponseModel>();


                                foreach (var t in entity.ProductComposites)
                                {
                                    try
                                    {
                                        var productCompositePrice = productCompositeList[t.ProductCompositeId].ProductCompositePrices.Single(tk => tk.Version == entity.Version);
                                        bool isOldCompForTransference = false;
                                        localDynamicExpressoParameters = decideClass.Calculate(localDynamicExpressoParameters, defaultRequestParameter.RuleParameters, request.OfferTypeId, entity.Version, productCompositePrice).GetAwaiter().GetResult();




                                        var discounts = t.Discounts?.Where(x => x.Price != "0").Select(k => new PriceModel<decimal>
                                        {
                                            Price = offerHelper.CalculatePrice(k.Price, productCompositePrice, localDynamicExpressoParameters, decideClass),
                                            Duration = new Common.Models.DurationModel
                                            {
                                                Duration = k.Duration.Duration,
                                                DurationType = k.Duration.DurationType,
                                            },
                                            DurationStart = new Common.Models.DurationModel
                                            {
                                                Duration = k.DurationStart.Duration,
                                                DurationType = k.DurationStart.DurationType,
                                            },
                                            IsPrimary = k.IsPrimary == true ? true : false,
                                        });

                                        if (t.Discounts != null && t.Discounts.Any(k => !k.IsPrimary.HasValue))
                                        {
                                            if (t.Discounts.Count() > 1)
                                            {
                                                t.Discounts.OrderByDescending(k => k.Duration.Duration).FirstOrDefault().IsPrimary = true;
                                            }
                                            else if (t.Discounts.Count() == 1)
                                            {
                                                t.Discounts.FirstOrDefault().IsPrimary = true;
                                            }
                                        }

                                        var oldCompositeId = Guid.Empty;
                                        decimal price = 0;
                                        var comp = _offerTypeConstService.CurrentCompositePriceList.FirstOrDefault(k => k.ProductCompositeId == t.ProductCompositeId);
                                        if (comp != null && comp.IsOldCompositeForTransference)
                                        {
                                            isOldCompForTransference = true;
                                            oldCompositeId = _offerTypeConstService.CurrentCompositePriceList.FirstOrDefault(k => k.ProductCompositeId == t.ProductCompositeId && k.IsOldCompositeForTransference).OldProductCompositeId;
                                        }

                                        //if (request.OfferTypeId == OfferType.TransferInternet.Id && _offerTypeConstService.CurrentCompositePriceList.Any(k => (k.ProductCompositeId == t.ProductCompositeId)))
                                        //{
                                        //    var comp = _offerTypeConstService.CurrentCompositePriceList.FirstOrDefault(k => k.ProductCompositeId == t.ProductCompositeId);
                                        //    price = comp.Price;
                                        //    discounts = comp.Discounts.OrderByDescending(x => x.EndDate).Select(k => new PriceModel<decimal>
                                        //    {
                                        //        Price = k.Price,
                                        //        Duration = (t.Discounts != null && t.Discounts.Count() > 0) ? new Common.Models.DurationModel
                                        //        {
                                        //            Duration = t.Discounts.FirstOrDefault().Duration.Duration,
                                        //            DurationType = t.Discounts.FirstOrDefault().Duration.DurationType,
                                        //        } : new Common.Models.DurationModel
                                        //        {
                                        //            Duration = 0,
                                        //            DurationType = DurationTypeEnum.Month,
                                        //        },
                                        //        DurationStart = (t.Discounts != null && t.Discounts.Count() > 0) ? new Common.Models.DurationModel
                                        //        {
                                        //            Duration = t.Discounts.FirstOrDefault().DurationStart.Duration,
                                        //            DurationType = t.Discounts.FirstOrDefault().DurationStart.DurationType,
                                        //        } : new Common.Models.DurationModel
                                        //        {
                                        //            Duration = 0,
                                        //            DurationType = DurationTypeEnum.Month,
                                        //        },
                                        //        IsPrimary = comp.Discounts.Count() > 1 ? comp.Discounts.Any(x => x.EndDate < k.EndDate) : true
                                        //    }).ToList();
                                        //    if (comp.IsOldCompositeForTransference == true)
                                        //    {
                                        //        isOldCompForTransference = true;
                                        //        oldCompositeId = _offerTypeConstService.CurrentCompositePriceList.FirstOrDefault(k => k.ProductCompositeId == t.ProductCompositeId && k.IsOldCompositeForTransference).OldProductCompositeId;
                                        //    }
                                        //}
                                        //else if (request.OfferTypeId == OfferType.TransferInternet.Id && _offerTypeConstService.CurrentCompositePriceList.Any(k => k.ProductSpecification.Id == productCompositeList[t.ProductCompositeId].ProductSpecification.Id && k.IsOldCompositeForTransference) && productCompositeList[t.ProductCompositeId].ProductSpecification.Id == ProductSpecificationEnum.Naked.Id)
                                        //{
                                        //    //nakil tekliflerinde cift indiirm olamaz o yüzden firs atıldı cift indirim nakilde olmamalı

                                        //    price = _offerTypeConstService.CurrentCompositePriceList.FirstOrDefault(k => k.ProductSpecification.Id == ProductSpecificationEnum.Naked.Id && k.IsOldCompositeForTransference).Price;
                                        //    discounts = _offerTypeConstService.CurrentCompositePriceList.FirstOrDefault(k => k.ProductSpecification.Id == ProductSpecificationEnum.Naked.Id && k.IsOldCompositeForTransference).Discounts.Select(k => new PriceModel<decimal>
                                        //    {
                                        //        Price = k.Price,
                                        //        Duration = (t.Discounts != null && t.Discounts.Count() > 0) ? new Common.Models.DurationModel
                                        //        {
                                        //            Duration = t.Discounts.FirstOrDefault().Duration.Duration,
                                        //            DurationType = t.Discounts.FirstOrDefault().Duration.DurationType,
                                        //        } : new Common.Models.DurationModel
                                        //        {
                                        //            Duration = 0,
                                        //            DurationType = DurationTypeEnum.Month,
                                        //        },
                                        //        DurationStart = (t.Discounts != null && t.Discounts.Count() > 0) ? new Common.Models.DurationModel
                                        //        {
                                        //            Duration = t.Discounts.FirstOrDefault().DurationStart.Duration,
                                        //            DurationType = t.Discounts.FirstOrDefault().DurationStart.DurationType,
                                        //        } : new Common.Models.DurationModel
                                        //        {
                                        //            Duration = 0,
                                        //            DurationType = DurationTypeEnum.Month,
                                        //        },
                                        //        IsPrimary = true
                                        //    }).ToList();
                                        //    isOldCompForTransference = true;
                                        //    oldCompositeId = _offerTypeConstService.CurrentCompositePriceList.FirstOrDefault(k => k.ProductSpecification.Id == ProductSpecificationEnum.Naked.Id && k.IsOldCompositeForTransference).OldProductCompositeId;

                                        //}
                                        //else if (productCompositeList[t.ProductCompositeId].ProductSpecification.Id == ProductSpecificationEnum.Naked.Id && localDynamicExpressoParameters.Any(x => x.Name == "CurrentNakedPrice"))
                                        if (productCompositeList[t.ProductCompositeId].ProductSpecification.Id == ProductSpecificationEnum.Naked.Id && localDynamicExpressoParameters.Any(x => x.Name == "CurrentNakedPrice"))
                                        {
                                            price = decimal.Parse(localDynamicExpressoParameters.FirstOrDefault(x => x.Name == "CurrentNakedPrice").Value);
                                            if (!(price > 0))
                                            {
                                                price = offerHelper.CalculatePrice(t.Price, productCompositePrice, localDynamicExpressoParameters, decideClass);
                                            }
                                        }
                                        else
                                        {
                                            price = offerHelper.CalculatePrice(t.Price, productCompositePrice, localDynamicExpressoParameters, decideClass);
                                        }

                                        //if (productCompositeList[t.ProductCompositeId].ProductSpecification.Id == ProductSpecificationEnum.Naked.Id && localDynamicExpressoParameters.Any(x => x.Name == "CurrentNakedPrice"))
                                        //{
                                        //    price = decimal.Parse(localDynamicExpressoParameters.FirstOrDefault(x => x.Name == "CurrentNakedPrice").Value);
                                        //    if (!(price > 0))
                                        //    {
                                        //        price = offerHelper.CalculatePrice(t.Price, productCompositePrice, localDynamicExpressoParameters, decideClass);
                                        //    }
                                        //}
                                        //else
                                        //{
                                        //    price = offerHelper.CalculatePrice(t.Price, productCompositePrice, localDynamicExpressoParameters, decideClass);
                                        //}

                                        var _newComposite = new ProductCompositeResponseModel
                                        {
                                            BillingProductCompositeId = productCompositeList[t.ProductCompositeId].BillingProductComposite.Id,
                                            ResourceCompositeBundleIds = productCompositeList[t.ProductCompositeId].ResourceCompositeBundles?.Select(asd => asd.Id).ToList(),

                                            ProductComposite = productCompositeList[t.ProductCompositeId].ProductComposite,
                                            ProductSpecification = productCompositeList[t.ProductCompositeId].ProductSpecification,
                                            //Price = offerHelper.CalculatePrice(t.Price, productCompositePrice, localDynamicExpressoParameters, decideClass),

                                            Price = price,
                                            ProductCompositePrice = new ProductCompositePriceResponseModel
                                            {
                                                ProductCompositePriceId = productCompositePrice.ProductCompositePriceId,
                                                ListPrice = productCompositePrice.ListPrice,
                                                MinPrice = productCompositePrice.MinPrice,
                                                WithoutContractPrice = productCompositePrice.WithoutContractPrice,
                                                Version = productCompositePrice.Version
                                            },
                                            Discounts = discounts.ToList(),
                                            ExtraPrices = t.ExtraPrices?.ToDictionary(axc => axc.Key, axcc => axcc.Value),

                                            ProductComponents = productCompositeList[t.ProductCompositeId].ProductComponents
                                            ?.Select(k => new ProductComponentResponseModel
                                            {
                                                ProductComponent = new Common.Models.IdNameModel<Guid>
                                                {
                                                    Id = productComponentList.First(l => l.ProductComponentId == k).ProductComponentId,
                                                    Name = productComponentList.First(l => l.ProductComponentId == k).Name
                                                },

                                                ProductComponentPrices = productComponentList.First(l => l.ProductComponentId == k).ProductComponentPrices
                                                ?.Where(tk => tk.Version == entity.Version)
                                                .Select(l => new ProductComponentPriceResponseModel
                                                {
                                                    ProductComponentPriceId = l.ProductComponentPriceId,
                                                    Version = l.Version,
                                                    ListPrice = l.ListPrice,
                                                    MinPrice = l.MinPrice,
                                                    WithoutContractPrice = l.WithoutContractPrice
                                                }).ToList(),

                                                ProductComponentCharacteristicValues = productComponentList.First(l => l.ProductComponentId == k).ProductSpecificationCharacteristicValues
                                                ?.Select(l => new ProductComponentCharacteristicValueResponseModel
                                                {
                                                    ProductSpecificationCharacteristic = new Common.Models.IdNameModel<Guid>
                                                    {
                                                        Id = productSpecificationCharacteristicList.First(m => m.ProductSpecificationCharacteristicValues.Any(n => n.ProductSpecificationCharacteristicValueId == l)).ProductSpecificationCharacteristicId,
                                                        Name = productSpecificationCharacteristicList.First(m => m.ProductSpecificationCharacteristicValues.Any(n => n.ProductSpecificationCharacteristicValueId == l)).Name
                                                    },
                                                    ProductSpecificationCharacteristicDescription = productSpecificationCharacteristicList.First(m => m.ProductSpecificationCharacteristicValues.Any(n => n.ProductSpecificationCharacteristicValueId == l)).Description,
                                                    ProductSpecificationCharacteristicValueId = l,
                                                    Value = productSpecificationCharacteristicList.First(m => m.ProductSpecificationCharacteristicValues.Any(n => n.ProductSpecificationCharacteristicValueId == l)).ProductSpecificationCharacteristicValues.First(n => n.ProductSpecificationCharacteristicValueId == l).Value,
                                                    ValueType = productSpecificationCharacteristicList.First(m => m.ProductSpecificationCharacteristicValues.Any(n => n.ProductSpecificationCharacteristicValueId == l)).ProductSpecificationCharacteristicValues.First(n => n.ProductSpecificationCharacteristicValueId == l).ValueType,
                                                    Description = productSpecificationCharacteristicList.First(m => m.ProductSpecificationCharacteristicValues.Any(n => n.ProductSpecificationCharacteristicValueId == l)).ProductSpecificationCharacteristicValues.First(n => n.ProductSpecificationCharacteristicValueId == l).Description,
                                                    UnitOfMeasure = productSpecificationCharacteristicList.First(m => m.ProductSpecificationCharacteristicValues.Any(n => n.ProductSpecificationCharacteristicValueId == l)).ProductSpecificationCharacteristicValues.First(n => n.ProductSpecificationCharacteristicValueId == l).UnitOfMeasure,
                                                }).ToList()

                                            }).ToList()
                                        };
                                        if (isOldCompForTransference)
                                        {
                                            _newComposite.OldProductCompositeId = oldCompositeId;
                                        }
                                        offer.ProductComposites.Add(_newComposite);

                                    }
                                    catch (Exception ex)
                                    {
                                        //TODO: burada oluşan hata loglanarak kontrol edilmeli.
                                    }


                                }

                                if (entity.PriceCalculationClass != null)
                                {
                                    object[] prms = new[] { requestParemeters.SingleOrDefault(df => df.Property == "CompositeType")?.Value, requestParemeters.SingleOrDefault(df => df.Property == "TvAmount")?.Value };
                                    priceCalcClass.Calculate(localDynamicExpressoParameters, entity.PriceCalculationClass, offer, entity.Version, request.OfferTypeId, prms);

                                }

                                offer.ProductComposites = offer.ProductComposites.Select(x => { x.TotalPrice = x.Discounts == null || x.Discounts.Count() == 0 ? x.Price : x.Price - x.Discounts.Where(k => k.IsPrimary.HasValue && k.IsPrimary.Value == true).Sum(y => y.Price); return x; }).ToList();
                                if (entity.BonusProductComposites != null && entity.BonusProductComposites.Any())
                                    offerHelper.FillBonusProductComposites(offer, entity.BonusProductComposites?.ToList(), entity.Version, productDic, productCompositeDic, productComponentDic, productSpecificationCharacteristicDic);

                                responseData.Add(offer);
                                if (request.OfferTypeId == OfferType.TransferInternet.Id)
                                {

                                    if (offer.ProductComposites.All(x => _offerTypeConstService.CurrentCompositePriceList.Select(x => x.ProductCompositeId).Contains(x.ProductComposite.Id) || _offerTypeConstService.CurrentCompositePriceList.Select(x => x.ProductCompositeId).Contains(x.OldProductCompositeId)))
                                    {
                                        foreach (var item in _offerTypeConstService.CurrentCompositePriceList)
                                        {
                                            var newProd = offer.ProductComposites.FirstOrDefault(x => x.ProductSpecification.Id == item.ProductSpecification.Id);
                                            if (newProd != null)
                                            {
                                                offer.ProductComposites.FirstOrDefault(x => x.ProductSpecification.Id == item.ProductSpecification.Id).Price = item.Price;

                                                if (offer.ProductComposites.FirstOrDefault(x => x.ProductSpecification.Id == item.ProductSpecification.Id).Discounts.Count() > 0 && item.Discounts != null && item.Discounts.Count() > 0)
                                                {
                                                    offer.ProductComposites.FirstOrDefault(x => x.ProductSpecification.Id == item.ProductSpecification.Id).Discounts.OrderByDescending(k => k.Price).FirstOrDefault().Price = item.Discounts.OrderByDescending(k => k.Price).FirstOrDefault().Price;
                                                    if (item.Discounts.Count() > 1 && newProd.Discounts.Count() < 2)
                                                    {
                                                        offer.ProductComposites.FirstOrDefault(x => x.ProductSpecification.Id == item.ProductSpecification.Id).Discounts.Add(new PriceModel<decimal>
                                                        {
                                                            Price = item.Discounts.OrderByDescending(k => k.Price).LastOrDefault().Price,
                                                            Duration = new Common.Models.DurationModel
                                                            {
                                                                Duration = 0,
                                                                DurationType = DurationTypeEnum.Month,
                                                            },
                                                            DurationStart = new Common.Models.DurationModel
                                                            {
                                                                Duration = 0,
                                                                DurationType = DurationTypeEnum.Month,
                                                            },
                                                            IsPrimary = false
                                                        });
                                                    }
                                                    offer.ProductComposites.FirstOrDefault(x => x.ProductSpecification.Id == item.ProductSpecification.Id).Discounts.OrderByDescending(k => k.Price).LastOrDefault().Price = item.Discounts.OrderByDescending(k => k.Price).LastOrDefault().Price;
                                                    if (item.Discounts.Count() == 1)
                                                    {
                                                        offer.ProductComposites.FirstOrDefault(x => x.ProductSpecification.Id == item.ProductSpecification.Id).Discounts.LastOrDefault().IsPrimary = true;
                                                    }
                                                    else
                                                    {
                                                        offer.ProductComposites.FirstOrDefault(x => x.ProductSpecification.Id == item.ProductSpecification.Id).Discounts.OrderByDescending(x => x.Price).FirstOrDefault().IsPrimary = true;
                                                        offer.ProductComposites.FirstOrDefault(x => x.ProductSpecification.Id == item.ProductSpecification.Id).Discounts.OrderByDescending(x => x.Price).LastOrDefault().IsPrimary = false;
                                                    }

                                                }
                                                else if (offer.ProductComposites.FirstOrDefault(x => x.ProductSpecification.Id == item.ProductSpecification.Id).Discounts.Count() > 0 && (item.Discounts == null || item.Discounts.Count() == 0))
                                                {
                                                    offer.ProductComposites.FirstOrDefault(x => x.ProductSpecification.Id == item.ProductSpecification.Id).Discounts = new List<PriceModel<decimal>>();
                                                }

                                            }
                                        }
                                        offer.IsCurrentOffer = true;
                                    }

                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Search Add {ProductOfferId}", entity.ProductOffer.Id);
                            }
                        }
                    }



                    if (request.CharacteristicList.Any(x => x.FirstOrDefault(k => k.Property == "Provider") != null && x.FirstOrDefault(k => k.Property == "Provider").Value == "TT"))
                    {
                        if (request.CharacteristicList.Any(x => x.FirstOrDefault(k => k.Property == "Provider") != null && x.FirstOrDefault(k => k.Property == "Provider").Value == "SOL"))
                        {

                        }
                        else
                        {
                            var withoutNakedSol = responseData.Where(x => !x.ProductComposites.Any(k => k.ProductComponents.Any(t => t.ProductComponentCharacteristicValues.Any(y => y.Value.ToLower() == "sol")))).ToList();
                            responseData = new ConcurrentBag<ProductOfferResponseModel>(withoutNakedSol);
                        }

                    }

                    if (request.ProductSpecifications.Any(x => x.Id == ProductSpecificationEnum.Internet.Id))
                    {
                        if (request.CharacteristicList.Any(x => x.FirstOrDefault(k => k.Property == "MaxSpeed") != null))
                        {
                            var speed = Convert.ToInt32(request.CharacteristicList.SelectMany(x => x).FirstOrDefault(k => k.Property == "MaxSpeed").Value);
                            if (speed > 12287)
                            {
                                var data = responseData.Where(x => !x.ProductComposites.Any(x => x.ProductComposite.Id == Guid.Parse("34651b79-fbd1-4b1f-b7c2-28f140f93d87") || x.ProductComposite.Id == Guid.Parse("eab7681d-2c9a-42be-b20f-4f081862b1d4") || x.ProductComposite.Id == Guid.Parse("f36d4e4c-57d3-4c4a-b359-90a2aa83b004")));
                                responseData = new ConcurrentBag<ProductOfferResponseModel>(data);
                            }
                        }
                    }
                    //bu offerdaki bu product in cevabi
                    ConcurrentDictionary<Tuple<Guid, Guid>, ProductOfferResponseModel> calculatedOffersDict = new ConcurrentDictionary<Tuple<Guid, Guid>, ProductOfferResponseModel>();

                    Parallel.ForEach(responseData/*, new ParallelOptions { MaxDegreeOfParallelism = 1 }*/, item =>
                    {
                        try
                        {
                            List<DynamicExpressoParameterModel> localDynamicExpressoParameters = new List<DynamicExpressoParameterModel>();

                            foreach (var dyn in dynamicExpressoParameters)
                            {
                                localDynamicExpressoParameters.Add(new DynamicExpressoParameterModel
                                {
                                    Name = dyn.Name,
                                    Type = dyn.Type,
                                    Value = dyn.Value
                                });
                            }
                            if (offerDecoratorList.ContainsKey(item.ProductOffer.Id))
                            {
                                var decoratorClass = offerDecoratorClassDict[item.ProductOffer.Id];
                                //var foundOffer = decoratorClass.Calculate(calculatedOffersDict, request, item, item.Version, item.ProductOfferCatalog.Id, item.BundleProduct.Id, localDynamicExpressoParameters).GetAwaiter().GetResult();
                            }

                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Search decoratorClass {@Request}", request);
                        }
                    });
                }


                //go+internet fix
             
                var offerResponseData = FixBundleOffersPayTvDiscountsPrices(responseData);




                var response = new ApiSearchResponse<ProductOfferResponseModel>
                {
                    IsSuccess = true,
                };
                response.Data.Items = offerResponseData;
                response.Data.PageSize = responseData?.Count ?? 0;
                response.Data.TotalCount = responseData?.Count ?? 0;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Search {@Request}", request);

                var response = new ApiSearchResponse<ProductOfferResponseModel>
                {
                    IsSuccess = false,
                    Message = ex.Message + " - " + ex.InnerException?.Message

                };
                return Ok(response);
            }
        }

        [HttpPost(ProductOfferingApiMethodConst.ProductOffer.Search)]
        [ProducesResponseType(200, Type = typeof(ApiSearchResponse<ProductOfferResponseModel>))]
        public async Task<IActionResult> Search([FromBody] ProductOfferRequestModel request)
        {

            try
            {
                //_logger.LogInformation(Newtonsoft.Json.JsonConvert.SerializeObject(request));

                var provider = _cacheProvider.GetCachingProvider("DefaultInMemory");
                if (!await provider.ExistsAsync("ProductOfferProductMapCache"))
                {
                    //set cache
                    await _cachingHelper.ResetCacheOfferSearch();
                }

                
                
                var productOfferProductMapDic = (await provider.GetAsync<Dictionary<Guid, ProductOfferProductMap>>("ProductOfferProductMapCache")).Value.Values;
                var productDic = (await provider.GetAsync<Dictionary<Guid, Product>>("ProductCache")).Value.Values;
                var productOfferDic = (await provider.GetAsync<Dictionary<Guid, ProductOffer>>("ProductOfferCache")).Value.Values;
                var productOfferCatalogDic = (await provider.GetAsync<Dictionary<Guid, ProductOfferCatalog>>("ProductOfferCatalogCache")).Value.Values;

                var productCompositeDic = (await provider.GetAsync<Dictionary<Guid, ProductComposite>>("ProductCompositeCache")).Value.Values;
                var productComponentDic = (await provider.GetAsync<Dictionary<Guid, ProductComponent>>("ProductComponentCache")).Value.Values;
                var productOfferRuleDic = (await provider.GetAsync<Dictionary<Guid, ProductOfferRule>>("ProductOfferRuleCache")).Value;
                var productSpecificationCharacteristicDic = (await provider.GetAsync<Dictionary<Guid, ProductSpecificationCharacteristic>>("ProductSpecificationCharacteristicCache")).Value.Values;
                var requestParameterDic = (await provider.GetAsync<Dictionary<string, RequestParameter>>("RequestParameterCache")).Value.Values;
                var campaignDetailsDic = (await provider.GetAsync<Dictionary<Guid, CampaignDetails>>("CampaignDetailsCache")).Value;
                var productCompositeEquivalentDic = (await provider.GetAsync<Lookup<Guid, ProductCompositeEquivalents>>("ProductCompositeEquivalentsCache")).Value;

                var characteristicList = await CharacteristicsParametersSplitterHelper.SplitCharacteristics(request.CharacteristicList, _mongoRepository);
                var requestParemeters = await CharacteristicsParametersSplitterHelper.SplitParameters(request.CharacteristicList, _mongoRepository);
                var productSpecifications = CharacteristicsParametersSplitterHelper.SplitSpecifications(request.ProductSpecifications, request.CharacteristicList, request.OfferTypeId);

                var versions = VersionHelper.Find(request.OfferTypeId, productSpecifications);


                //sadece aktif productlari aktifler
                productDic = productDic.Where(x => !x.Disabled).ToDictionary(x => x.ProductId).Values;

                //offer type anlamak için lazım.
                _offerTypeConstService.RequestedProductSpecifications = request.ProductSpecifications;
                bool addGoproduct = false;

                //go+internet fix, paytv yoksa ve go varsa intenettle beraber, sadece internet gibi gor
                if (_offerTypeConstService.RequestedProductSpecifications.Contains(ProductSpecificationEnum.Internet) && !_offerTypeConstService.RequestedProductSpecifications.Contains(ProductSpecificationEnum.PayTv)
                    && _offerTypeConstService.RequestedProductSpecifications.Contains(ProductSpecificationEnum.Go) && (request.OfferTypeId == OfferType.SolSwap.Id || request.OfferTypeId == OfferType.Raise.Id || request.OfferTypeId == OfferType.Renew.Id || request.OfferTypeId == OfferType.Upgrade.Id))
                {
                    _offerTypeConstService.RequestedProductSpecifications.Remove(ProductSpecificationEnum.Go);
                    addGoproduct = true;
                }
                var grupList = UserGroupIds;
                if (UserId == Guid.Parse("beaee219-beb9-4617-93bf-68e71d43da61"))
                {
                    grupList = new List<int> { 1, 70, 60, 84, 607, 610, 609, 85, 51, 72 };
                }
                var query = from offerMap in productOfferProductMapDic
                            join product in productDic on offerMap.ProductId equals product.ProductId
                            join offer in productOfferDic on offerMap.ProductOfferId equals offer.ProductOfferId
                            join catalog in productOfferCatalogDic on offer.ProductOfferCatalogId equals catalog.ProductOfferCatalogId
                            where catalog.SalesChannels.Any(x => x == request.SalesChannel)
                              && catalog.OfferForms.Contains(request.OfferFormId)
                              && grupList.Any(x => catalog.Groups.Contains(x))
                              && versions.Contains(offerMap.Version)
                              && offerMap.StartDate <= DateTime.Now
                              && offerMap.EndDate >= DateTime.Now
                              && product.StartDate <= DateTime.Now
                              && product.EndDate >= DateTime.Now
                              && catalog.StartDate <= DateTime.Now
                              && catalog.EndDate >= DateTime.Now
                              && offer.StartDate <= DateTime.Now
                              && offer.EndDate >= DateTime.Now
                            //&& offer.OfferType.Id == OfferType.ReverseCrossFromInternetToTv.Id
                            //&& offerMap.ProductOfferId == Guid.Parse("274cf35e-0e71-4f72-8dc7-9e6564172cf0")
                            //&& offerMap.ProductId == Guid.Parse("f4ace640-a9e7-461f-af2a-45c7bc3c9535")
                            //&& new List<Guid> { Guid.Parse("a7791360-b819-40be-8fdc-5c36b157cade"), Guid.Parse("d8cb29ab-bd99-458a-b772-d187f2ae4ead"), Guid.Parse("0406397c-e88c-4d0d-b496-a32ee0bfb43e") }.Contains( offer.ProductOfferCatalogId)
                            //&& offerMap.ProductComposites.Any(x=>x.ProductCompositeId == Guid.Parse("09ce76fc-b3c8-48c5-b69d-9167fe8b7797"))
                            select new
                            {
                                offerMap.ProductOfferProductMapId,
                                offerMap.Version,
                                offerMap.Rules,
                                Product = new Common.Models.IdNameModel<Guid> { Id = product.ProductId, Name = product.Name },
                                ProductOffer = new Common.Models.IdNameModel<Guid> { Id = offer.ProductOfferId, Name = offer.Name },
                                ProductOfferCatalog = new Common.Models.IdNameModel<Guid> { Id = catalog.ProductOfferCatalogId, Name = catalog.Name },
                                OfferRoutingType = offer.OfferRoutingType,
                                Term = new Common.Models.DurationModel
                                {
                                    Duration = offer.Term.Duration,
                                    DurationType = offer.Term.DurationType,
                                },
                                OTFs = offer.OTFs,
                                ProductComposites = offerMap.ProductComposites,
                                ProductOfferRules = offer.ProductOfferRules,
                                offer.PaymentType,
                                offer.FeeType,
                                offer.InstallmentsCount,
                                offer.StbOwnerOnly,
                                offerMap.BonusProductComposites,
                                CampaignDetailId = offer.CampaignDetailId ?? Guid.Empty,
                                offer.PriceCalculationClass,
                                offer.OfferType,
                                offer.ProductOfferDecorators
                            };

                
                if (!string.IsNullOrEmpty(request.CouponCode))
                {
                    var coupon = await _mongoRepository.GetAsync<Coupons>(x => x.Code == request.CouponCode);
                    if (coupon.Count > coupon.Used && coupon.EndDate.Date <= DateTime.Now.Date && coupon.StartDate.Date >= DateTime.Now.Date)
                    {
                        if (coupon.ProductOfferProductMapId.HasValue)
                        {
                            query = query.Where(x => x.ProductOfferProductMapId == coupon.ProductOfferProductMapId);
                        }
                        else if (coupon.ProductOfferId.HasValue)
                        {
                            query = query.Where(x => x.ProductOffer.Id == coupon.ProductOfferId);
                        }
                        else if (coupon.ProductOfferCatalogId.HasValue)
                        {
                            query = query.Where(x => x.ProductOfferCatalog.Id == coupon.ProductOfferCatalogId);
                        }
                        else
                        {
                            //kupon kodu hatalı. response boş olmalı.
                            return Ok(new ApiResponse("No any offers to depend this Coupon Code!"));
                        }
                    }
                    else
                    {
                        return Ok(new ApiResponse("Invalid Coupon Code!"));
                    }
                }

                var searchResult = query.ToList();
                //searchResult = searchResult.Where(x => x.ProductOffer.Name.Contains("2301004") && x.Product.Name.ToLower().Contains("fiber vdsl 24mb ekadar (fttb) - limitsiz") && x.Product.Name.ToLower().Contains("yalin")).ToList();
                if (productSpecifications != null && productSpecifications.Any())
                {
                    var requestProductSpecificationIds = productSpecifications.Select(a => a.Id).ToList();
                    var productCompositeFilterIds = productCompositeDic.Where(x => requestProductSpecificationIds.Contains(x.ProductSpecification.Id)).Select(x => x.ProductCompositeId).ToList();
                    searchResult = searchResult.Where(x => x.ProductComposites.Count == productSpecifications.Count).ToList();
                    searchResult = searchResult.Where(x => x.ProductComposites.All(t => productCompositeFilterIds.Contains(t.ProductCompositeId))).ToList();
                }

                var responseData = new ConcurrentBag<ProductOfferResponseModel>();
                if (searchResult != null && searchResult.Any())
                {
                    var productCompositeIds = searchResult.SelectMany(x => x.ProductComposites).Select(x => x.ProductCompositeId).Distinct().ToList();

                    var productCompositeList = productCompositeDic.Where(x => productCompositeIds.Contains(x.ProductCompositeId))
                                                .Select(composite => new
                                                {
                                                    ProductComposite = new Common.Models.IdNameModel<Guid> { Id = composite.ProductCompositeId, Name = composite.Name },
                                                    composite.ProductSpecification,
                                                    composite.ProductComponents,
                                                    composite.BillingProductComposite,
                                                    composite.ResourceCompositeBundles,
                                                    composite.ProductCompositePrices,
                                                    composite.Neighborhoods
                                                }).ToDictionary(x => x.ProductComposite.Id);


                    var productComponentIds = productCompositeList.SelectMany(x => x.Value.ProductComponents).Distinct().ToList();
                    var productComponentList = productComponentDic.Where(x => productComponentIds.Contains(x.ProductComponentId)).ToList();


                    var productSpecificationCharacteristicValueIds = productComponentList.SelectMany(x => x.ProductSpecificationCharacteristicValues).Distinct().ToList();
                    var productSpecificationCharacteristicList = productSpecificationCharacteristicDic.Where(x => x.ProductSpecificationCharacteristicValues.Any(t => productSpecificationCharacteristicValueIds.Contains(t.ProductSpecificationCharacteristicValueId))).ToList();

                    if (characteristicList != null && characteristicList.Any())
                    {
                        var filteredCompositeIdList = new List<Guid>();
                        foreach (var characteristics in characteristicList)
                        {
                            var filteredCharacteristicIdList = characteristics.SelectMany(x =>
                                        productSpecificationCharacteristicList
                                                .First(t => t.Name == x.Property)
                                                .ProductSpecificationCharacteristicValues
                                                .Where(z => z.Value == x.Value)?
                                                .Select(sa => new { x.Property, sa.ProductSpecificationCharacteristicValueId }))
                                       .ToList();

                            filteredCompositeIdList.AddRange(
                                productCompositeList.Where(x =>
                                    filteredCharacteristicIdList.GroupBy(k => k.Property).All(kk =>
                                             x.Value.ProductComponents.Select(xa => productComponentList.First(xat => xat.ProductComponentId == xa))
                                                        .SelectMany(xt => xt.ProductSpecificationCharacteristicValues)
                                                        .Any(t => kk.Any(saa => saa.ProductSpecificationCharacteristicValueId == t))))
                                .Select(x => x.Value.ProductComposite.Id).ToList());
                            var aa = productCompositeList.Where(x =>
                                     filteredCharacteristicIdList.GroupBy(k => k.Property).All(kk =>
                                              x.Value.ProductComponents.Select(xa => productComponentList.First(xat => xat.ProductComponentId == xa))
                                                         .SelectMany(xt => xt.ProductSpecificationCharacteristicValues)
                                                         .Any(t => kk.Any(saa => saa.ProductSpecificationCharacteristicValueId == t))))
                                .Select(x => x.Value.ProductComposite.Id).ToList();
                        }
                        searchResult = searchResult
                            .Where(x => x.ProductComposites
                                        .Any(t => filteredCompositeIdList.Contains(t.ProductCompositeId)))
                            .ToList();
                    }


                    var dynamicExpressoParameters = CharacteristicsParametersSplitterHelper.SplitDynamicExpressoParametersFromRequestModel(request.CharacteristicList);
                    if (productSpecifications.Any() && productSpecifications.Any(x => x.Id == ProductSpecificationEnum.PayTv.Id))
                    {
                        var transmissionFee = await _mongoRepository.GetAsync<Fee>(x => x.Name == "İletim Bedeli" && (x.ValidFrom <= DateTime.Now && x.ValidThru >= DateTime.Now));
                        if (transmissionFee != null)
                        {
                            dynamicExpressoParameters.Add(new DynamicExpressoParameterModel
                            {
                                Name = "TransmissionFee",
                                Type = "System.Decimal",
                                Value = transmissionFee.Price.ToString()
                            });
                        }
                    }

                    if (requestParemeters != null && requestParemeters.Any())
                    {
                        var properties = requestParemeters.Select(a => a.Property);
                        var requestParameters = requestParameterDic.Where(x => properties.Contains(x.Name)).ToList();
                        if (requestParameters != null && requestParameters.Any())
                        {
                            foreach (var requestParameter in requestParameters)
                            {
                                var decideClass1 = ActivatorUtilities.CreateInstance(_provider, Type.GetType(requestParameter.CalculationClass)) as IGetParameters;
                                foreach (var version in versions)
                                {
                                    dynamicExpressoParameters = (await decideClass1.Calculate(dynamicExpressoParameters, requestParameter.RuleParameters, request.OfferTypeId, version, requestParemeters.First(df => df.Property == requestParameter.Name).Value));
                                }
                            }
                        }
                    }

                    #region Taahütsüz upgrade teklifleri

                    //var defaultRequestParameterRegistered = dynamicExpressoParameters.FirstOrDefault(x => x.Name == "IsUnRegistered");

                    //if (request.OfferTypeId == OfferType.Upgrade.Id && defaultRequestParameterRegistered?.Value == "True")
                    //{
                    //   searchResult = searchResult.Where(x=>x.ProductOffer.Name.Contains("Taahhütsüz") && x.Term.Duration==0).ToList();
                    //}

                    #endregion

                    var defaultRequestParameter = requestParameterDic.First(x => x.Name == "Default");
                    var decideClass = ActivatorUtilities.CreateInstance(_provider, Type.GetType(defaultRequestParameter.CalculationClass)) as IGetParameters;

                    //do not show same product when upgrade offer
                    if (_offerTypeConstService.OfferTypes.Select(xa => xa.Id).Contains(OfferType.Upgrade.Id))
                    {
                        searchResult = searchResult.Where(x => x.Product.Id != _offerTypeConstService.ProductId)?.ToList();
                    }

                    //must show same productComposite with cross offers when current product contains paytv 
                    if (
                            (
                                _offerTypeConstService.OfferTypes.Select(xa => xa.Id).Contains(OfferType.CrossFromTvToInternet.Id) ||
                                _offerTypeConstService.OfferTypes.Select(xa => xa.Id).Contains(OfferType.TransferInternet.Id)
                            ) &&
                            _offerTypeConstService.CurrentProductSpecifications.Any(aa => aa.Id == ProductSpecificationEnum.PayTv.Id))
                    {
                        var currentPayTvProductCompositeId = _offerTypeConstService.CurrentCompositePriceList.First(x => x.ProductSpecification.Id == ProductSpecificationEnum.PayTv.Id).ProductCompositeId;

                        //eslenigi varsa onu al
                        currentPayTvProductCompositeId = productCompositeEquivalentDic[currentPayTvProductCompositeId]?.SingleOrDefault(x => versions.Contains(x.Version))?.NewProductCompositeId ?? currentPayTvProductCompositeId;

                        searchResult = searchResult.Where(x => x.ProductComposites.Any(aa => aa.ProductCompositeId == currentPayTvProductCompositeId))?.ToList();
                    }

                    //must show same productComposite with raise offers 
                    if (_offerTypeConstService.OfferTypes.Select(xa => xa.Id).Contains(OfferType.Raise.Id))
                    {
                        //Naked için filtre iptal edildi.
                        foreach (var item in _offerTypeConstService.CurrentCompositePriceList.Where(x => x.ProductSpecification.Id != 5 && x.ProductSpecification.Id != ProductSpecificationEnum.Go.Id).ToList())
                        {
                            item.ProductCompositeId = productCompositeEquivalentDic[item.ProductCompositeId]?.SingleOrDefault(x => versions.Contains(x.Version))?.NewProductCompositeId ?? item.ProductCompositeId;

                            searchResult = searchResult.Where(x => x.ProductComposites.Any(aa => aa.ProductCompositeId == item.ProductCompositeId))?.ToList();
                        }

                    }


                    //Composite black list when change offer
                    if (_offerTypeConstService.OfferTypes.Select(xa => xa.Id).Contains(OfferType.Upgrade.Id)
                        || _offerTypeConstService.OfferTypes.Select(xa => xa.Id).Contains(OfferType.CrossFromTvToInternet.Id)
                        || _offerTypeConstService.OfferTypes.Select(xa => xa.Id).Contains(OfferType.CrossFromInternetToTv.Id)
                         || _offerTypeConstService.OfferTypes.Select(xa => xa.Id).Contains(OfferType.CrossFromInternetToInternetGo.Id)
                        || _offerTypeConstService.OfferTypes.Select(xa => xa.Id).Contains(OfferType.ReverseCrossFromTvToInternet.Id)
                        || _offerTypeConstService.OfferTypes.Select(xa => xa.Id).Contains(OfferType.ReverseCrossFromInternetToTv.Id))
                    {
                        //eslenigi varsa onu al
                        var currentCompositeIds = _offerTypeConstService.CurrentCompositePriceList?.Select(x => productCompositeEquivalentDic[x.ProductCompositeId]?.SingleOrDefault(x => versions.Contains(x.Version))?.NewProductCompositeId ?? x.ProductCompositeId)?.ToList();

                        if (currentCompositeIds != null && currentCompositeIds.Count > 0)
                        {
                            var blackList = productCompositeDic.Where(x => currentCompositeIds.Contains(x.ProductCompositeId) && x.BlackProductCompositeListWhenChangeOffer != null && x.BlackProductCompositeListWhenChangeOffer.Any())?.SelectMany(x => x.BlackProductCompositeListWhenChangeOffer)?.ToList();

                            if (blackList != null && blackList.Count > 0)
                            {
                                searchResult = searchResult.Where(x => !x.ProductComposites.Any(t => blackList.Contains(t.ProductCompositeId)))?.ToList();
                            }
                        }
                    }
                    if (request.OfferTypeId == OfferType.CrossFromInternetToInternetGo.Id)
                    {
                        searchResult = searchResult.Where(x => x.OfferType.Id == request.OfferTypeId).ToList();

                    }
                    else
                    {
                        searchResult = searchResult.Where(x => _offerTypeConstService.OfferTypes.Select(xa => xa.Id).Contains(x.OfferType.Id)).ToList();
                    }

                    var priceClassDict = new Dictionary<string, IPriceCalculator>();

                    foreach (var className in searchResult.Where(x => x.PriceCalculationClass != null).Select(x => x.PriceCalculationClass.ClassName).Distinct())
                    {
                        var priceCalcClass = ActivatorUtilities.CreateInstance(_provider, Type.GetType(className)) as IPriceCalculator;

                        priceClassDict.Add(className, priceCalcClass);
                    }

                    var offerRuleList = searchResult.Where(x => x.ProductOfferRules != null && x.ProductOfferRules.Any()).GroupBy(x => x.ProductOffer.Id).Select(x => new { x.First().ProductOfferRules, ProductOfferId = x.Key });

                    Dictionary<Guid, bool> offerRuleResultDict = new Dictionary<Guid, bool>();

                    //offerRules
                    foreach (var entity in offerRuleList)
                    {
                        foreach (var productOfferRuleId in entity.ProductOfferRules)
                        {
                            if (!offerRuleResultDict.ContainsKey(productOfferRuleId))
                            {
                                var productOfferRule = productOfferRuleDic[productOfferRuleId];
                                if (!bool.Parse(DynamicExpressoHelper.EvaluateAsync(new DynamicExpressoModel { Script = productOfferRule.Formula, Parameters = dynamicExpressoParameters }).ToString()))
                                {
                                    searchResult = searchResult.Where(x => x.ProductOffer.Id != entity.ProductOfferId).ToList();
                                    offerRuleResultDict.Add(productOfferRuleId, false);
                                    break;
                                }
                                else
                                    offerRuleResultDict.Add(productOfferRuleId, true);
                            }
                            else
                            {
                                if (!offerRuleResultDict[productOfferRuleId])
                                {
                                    searchResult = searchResult.Where(x => x.ProductOffer.Id != entity.ProductOfferId).ToList();
                                    break;
                                }
                            }
                        }
                    }

                    var offerDecoratorList = searchResult.Where(x => x.ProductOfferDecorators != null && x.ProductOfferDecorators.Any(y => y.Version == x.Version)).GroupBy(x => x.ProductOffer.Id).Select(x => new { x.First().ProductOfferDecorators.Single(y => y.Version == x.First().Version).ClassName, ProductOfferId = x.Key }).ToDictionary(x => x.ProductOfferId);

                    var offerDecoratorClassDict = new Dictionary<Guid, IOfferDecorator>();

                    foreach (var x in offerDecoratorList)
                    {
                        var decoratorClass = ActivatorUtilities.CreateInstance(_provider, Type.GetType(x.Value.ClassName)) as IOfferDecorator;

                        offerDecoratorClassDict.Add(x.Key, decoratorClass);
                    }

                    var offerHelper = new OfferHelper(_logger, _offerTypeConstService, _provider, _cacheProvider);



                    foreach (var entity in searchResult)

                    {
                        bool isRulesSucceed = true;
                        List<DynamicExpressoParameterModel> localDynamicExpressoParameters = new List<DynamicExpressoParameterModel>();

                        foreach (var item in dynamicExpressoParameters)
                        {
                            localDynamicExpressoParameters.Add(new DynamicExpressoParameterModel
                            {
                                Name = item.Name,
                                Type = item.Type,
                                Value = item.Value
                            });
                        }
                        try
                        {
                            #region Check Offer Rules

                            var productCompositePrices = entity.ProductComposites.Select(t => productCompositeList[t.ProductCompositeId].ProductCompositePrices.Single(x => x.Version == entity.Version)).ToList();

                            localDynamicExpressoParameters = decideClass.Calculate(localDynamicExpressoParameters, defaultRequestParameter.RuleParameters, request.OfferTypeId, entity.Version, productCompositePrices).GetAwaiter().GetResult();

                            if (entity.Rules != null && entity.Rules.Any())
                            {
                                foreach (var productOfferRuleId in entity.Rules)
                                {
                                    if (isRulesSucceed)
                                    {
                                        var productOfferRule = productOfferRuleDic[productOfferRuleId];

                                        //4 e 40 tarifelerde intenet olmamasi kosulu var. Ama upgrade yaparken veya intenete paytv satarken secilebilir olmali. Tabi mevcut urunlerinde boyle bir sey varsa.
                                        if ((request.OfferTypeId == OfferType.Upgrade.Id || request.OfferTypeId == OfferType.CrossFromInternetToTv.Id) && productOfferRule.Formula.Contains("ChurnDuration") && _offerTypeConstService.CurrentMainUntouchedComposites.Any(x => entity.ProductComposites.Any(y => y.ProductCompositeId == x)))
                                        {

                                        }
                                        else
                                            isRulesSucceed = isRulesSucceed && bool.Parse(DynamicExpressoHelper.EvaluateAsync(new DynamicExpressoModel { Script = productOfferRule.Formula, Parameters = localDynamicExpressoParameters }).ToString());
                                    }
                                }
                            }



                            if (entity.BonusProductComposites != null && entity.BonusProductComposites.Any())
                            {
                                List<BonusProductComposite> removeBonusList = new List<BonusProductComposite>();

                                foreach (var bonusProductComposite in entity.BonusProductComposites)
                                {
                                    if (!string.IsNullOrEmpty(bonusProductComposite.Rule) && !bool.Parse(DynamicExpressoHelper.EvaluateAsync(new DynamicExpressoModel { Script = bonusProductComposite.Rule, Parameters = localDynamicExpressoParameters }).ToString()))
                                        removeBonusList.Add(bonusProductComposite);
                                }

                                foreach (var item in removeBonusList)
                                {
                                    entity.BonusProductComposites.Remove(item);
                                }
                            }

                            #endregion

                            //check  Neighborhood
                            if (request.NeighborhoodCode.HasValue && request.NeighborhoodCode > 0)
                            {
                                foreach (var item in entity.ProductComposites)
                                {
                                    if (productCompositeList[item.ProductCompositeId].Neighborhoods != null && productCompositeList[item.ProductCompositeId].Neighborhoods.Count > 0)
                                    {
                                        if (!productCompositeList[item.ProductCompositeId].Neighborhoods.Select(xa => xa.Neighborhood.Id).Any(xa => xa == request.NeighborhoodCode.Value))
                                        {
                                            isRulesSucceed = false;
                                        }
                                    }
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            isRulesSucceed = false;
                        }

                        if (isRulesSucceed)
                        {
                            try
                            {
                                IPriceCalculator priceCalcClass = null;
                                if (entity.PriceCalculationClass != null)
                                {
                                    priceCalcClass = priceClassDict[entity.PriceCalculationClass.ClassName];
                                }

                                var offer = new ProductOfferResponseModel
                                {
                                    BundleProduct = entity.Product,
                                    ProductOffer = entity.ProductOffer,
                                    ProductOfferCatalog = entity.ProductOfferCatalog,
                                    OfferRoutingType = entity.OfferRoutingType,
                                    Term = entity.Term,
                                    PaymentType = entity.PaymentType,
                                    OfferType = entity.OfferType,
                                    InstallmentsCount = entity.InstallmentsCount,
                                    StbOwnerOnly = entity.StbOwnerOnly,
                                    FeeType = entity.FeeType,
                                    Version = entity.Version
                                };
                                if (entity.CampaignDetailId != Guid.Empty)
                                {
                                    var campaignDetail = campaignDetailsDic[entity.CampaignDetailId];
                                    offer.CampaignDetail = new CampaignDetailResponseModel
                                    {
                                        CampaignDetailId = campaignDetail.CampaignDetailId,
                                        Name = campaignDetail.Name,
                                        Form = campaignDetail.Form
                                    };
                                }

                                offer.OTFs = entity.OTFs
                                        ?.Select(t => new OTFModel<decimal>
                                        {
                                            Default = t.Default,
                                            OTFId = t.OTFId,
                                            OTFType = t.OTFType,
                                            Price = new PriceModel<decimal>
                                            {
                                                Price = decimal.Parse(t.Price.Price),
                                                Duration = new Common.Models.DurationModel
                                                {
                                                    Duration = t.Price.Duration.Duration,
                                                    DurationType = t.Price.Duration.DurationType,
                                                },
                                                DurationStart = new Common.Models.DurationModel
                                                {
                                                    Duration = t.Price.DurationStart.Duration,
                                                    DurationType = t.Price.DurationStart.DurationType,
                                                }
                                            }
                                        }).ToList();

                                offer.ProductOfferRules = entity.ProductOfferRules
                                        ?.Select(t => new Common.Models.IdNameModel<Guid>
                                        {
                                            Id = t,
                                            Name = productOfferRuleDic[t].Formula
                                        }).ToList();

                                offer.ProductComposites = new List<ProductCompositeResponseModel>();


                                foreach (var t in entity.ProductComposites)
                                {
                                    try
                                    {
                                        var productCompositePrice = productCompositeList[t.ProductCompositeId].ProductCompositePrices.Single(tk => tk.Version == entity.Version);
                                        bool isOldCompForTransference = false;
                                        localDynamicExpressoParameters = decideClass.Calculate(localDynamicExpressoParameters, defaultRequestParameter.RuleParameters, request.OfferTypeId, entity.Version, productCompositePrice).GetAwaiter().GetResult();




                                        var discounts = t.Discounts?.Where(x => x.Price != "0").Select(k => new PriceModel<decimal>
                                        {
                                            Price = offerHelper.CalculatePrice(k.Price, productCompositePrice, localDynamicExpressoParameters, decideClass),
                                            Duration = new Common.Models.DurationModel
                                            {
                                                Duration = k.Duration.Duration,
                                                DurationType = k.Duration.DurationType,
                                            },
                                            DurationStart = new Common.Models.DurationModel
                                            {
                                                Duration = k.DurationStart.Duration,
                                                DurationType = k.DurationStart.DurationType,
                                            },
                                            IsPrimary = k.IsPrimary == true ? true : false,
                                        });

                                        if (t.Discounts != null && t.Discounts.Any(k => !k.IsPrimary.HasValue))
                                        {
                                            if (t.Discounts.Count() > 1)
                                            {
                                                t.Discounts.OrderByDescending(k => k.Duration.Duration).FirstOrDefault().IsPrimary = true;
                                            }
                                            else if (t.Discounts.Count() == 1)
                                            {
                                                t.Discounts.FirstOrDefault().IsPrimary = true;
                                            }
                                        }

                                        var oldCompositeId = Guid.Empty;
                                        decimal price = 0;
                                        var comp = _offerTypeConstService.CurrentCompositePriceList.FirstOrDefault(k => k.ProductCompositeId == t.ProductCompositeId);
                                        if (comp != null && comp.IsOldCompositeForTransference)
                                        {
                                            isOldCompForTransference = true;
                                            oldCompositeId = _offerTypeConstService.CurrentCompositePriceList.FirstOrDefault(k => k.ProductCompositeId == t.ProductCompositeId && k.IsOldCompositeForTransference).OldProductCompositeId;
                                        }

                                        //if (request.OfferTypeId == OfferType.TransferInternet.Id && _offerTypeConstService.CurrentCompositePriceList.Any(k => (k.ProductCompositeId == t.ProductCompositeId)))
                                        //{
                                        //    var comp = _offerTypeConstService.CurrentCompositePriceList.FirstOrDefault(k => k.ProductCompositeId == t.ProductCompositeId);
                                        //    price = comp.Price;
                                        //    discounts = comp.Discounts.OrderByDescending(x => x.EndDate).Select(k => new PriceModel<decimal>
                                        //    {
                                        //        Price = k.Price,
                                        //        Duration = (t.Discounts != null && t.Discounts.Count() > 0) ? new Common.Models.DurationModel
                                        //        {
                                        //            Duration = t.Discounts.FirstOrDefault().Duration.Duration,
                                        //            DurationType = t.Discounts.FirstOrDefault().Duration.DurationType,
                                        //        } : new Common.Models.DurationModel
                                        //        {
                                        //            Duration = 0,
                                        //            DurationType = DurationTypeEnum.Month,
                                        //        },
                                        //        DurationStart = (t.Discounts != null && t.Discounts.Count() > 0) ? new Common.Models.DurationModel
                                        //        {
                                        //            Duration = t.Discounts.FirstOrDefault().DurationStart.Duration,
                                        //            DurationType = t.Discounts.FirstOrDefault().DurationStart.DurationType,
                                        //        } : new Common.Models.DurationModel
                                        //        {
                                        //            Duration = 0,
                                        //            DurationType = DurationTypeEnum.Month,
                                        //        },
                                        //        IsPrimary = comp.Discounts.Count() > 1 ? comp.Discounts.Any(x => x.EndDate < k.EndDate) : true
                                        //    }).ToList();
                                        //    if (comp.IsOldCompositeForTransference == true)
                                        //    {
                                        //        isOldCompForTransference = true;
                                        //        oldCompositeId = _offerTypeConstService.CurrentCompositePriceList.FirstOrDefault(k => k.ProductCompositeId == t.ProductCompositeId && k.IsOldCompositeForTransference).OldProductCompositeId;
                                        //    }
                                        //}
                                        //else if (request.OfferTypeId == OfferType.TransferInternet.Id && _offerTypeConstService.CurrentCompositePriceList.Any(k => k.ProductSpecification.Id == productCompositeList[t.ProductCompositeId].ProductSpecification.Id && k.IsOldCompositeForTransference) && productCompositeList[t.ProductCompositeId].ProductSpecification.Id == ProductSpecificationEnum.Naked.Id)
                                        //{
                                        //    //nakil tekliflerinde cift indiirm olamaz o yüzden firs atıldı cift indirim nakilde olmamalı

                                        //    price = _offerTypeConstService.CurrentCompositePriceList.FirstOrDefault(k => k.ProductSpecification.Id == ProductSpecificationEnum.Naked.Id && k.IsOldCompositeForTransference).Price;
                                        //    discounts = _offerTypeConstService.CurrentCompositePriceList.FirstOrDefault(k => k.ProductSpecification.Id == ProductSpecificationEnum.Naked.Id && k.IsOldCompositeForTransference).Discounts.Select(k => new PriceModel<decimal>
                                        //    {
                                        //        Price = k.Price,
                                        //        Duration = (t.Discounts != null && t.Discounts.Count() > 0) ? new Common.Models.DurationModel
                                        //        {
                                        //            Duration = t.Discounts.FirstOrDefault().Duration.Duration,
                                        //            DurationType = t.Discounts.FirstOrDefault().Duration.DurationType,
                                        //        } : new Common.Models.DurationModel
                                        //        {
                                        //            Duration = 0,
                                        //            DurationType = DurationTypeEnum.Month,
                                        //        },
                                        //        DurationStart = (t.Discounts != null && t.Discounts.Count() > 0) ? new Common.Models.DurationModel
                                        //        {
                                        //            Duration = t.Discounts.FirstOrDefault().DurationStart.Duration,
                                        //            DurationType = t.Discounts.FirstOrDefault().DurationStart.DurationType,
                                        //        } : new Common.Models.DurationModel
                                        //        {
                                        //            Duration = 0,
                                        //            DurationType = DurationTypeEnum.Month,
                                        //        },
                                        //        IsPrimary = true
                                        //    }).ToList();
                                        //    isOldCompForTransference = true;
                                        //    oldCompositeId = _offerTypeConstService.CurrentCompositePriceList.FirstOrDefault(k => k.ProductSpecification.Id == ProductSpecificationEnum.Naked.Id && k.IsOldCompositeForTransference).OldProductCompositeId;

                                        //}
                                        //else if (productCompositeList[t.ProductCompositeId].ProductSpecification.Id == ProductSpecificationEnum.Naked.Id && localDynamicExpressoParameters.Any(x => x.Name == "CurrentNakedPrice"))
                                        if (productCompositeList[t.ProductCompositeId].ProductSpecification.Id == ProductSpecificationEnum.Naked.Id && localDynamicExpressoParameters.Any(x => x.Name == "CurrentNakedPrice"))
                                        {
                                            price = decimal.Parse(localDynamicExpressoParameters.FirstOrDefault(x => x.Name == "CurrentNakedPrice").Value);
                                            if (!(price > 0))
                                            {
                                                price = offerHelper.CalculatePrice(t.Price, productCompositePrice, localDynamicExpressoParameters, decideClass);
                                            }
                                        }
                                        else
                                        {
                                            price = offerHelper.CalculatePrice(t.Price, productCompositePrice, localDynamicExpressoParameters, decideClass);
                                        }

                                        //if (productCompositeList[t.ProductCompositeId].ProductSpecification.Id == ProductSpecificationEnum.Naked.Id && localDynamicExpressoParameters.Any(x => x.Name == "CurrentNakedPrice"))
                                        //{
                                        //    price = decimal.Parse(localDynamicExpressoParameters.FirstOrDefault(x => x.Name == "CurrentNakedPrice").Value);
                                        //    if (!(price > 0))
                                        //    {
                                        //        price = offerHelper.CalculatePrice(t.Price, productCompositePrice, localDynamicExpressoParameters, decideClass);
                                        //    }
                                        //}
                                        //else
                                        //{
                                        //    price = offerHelper.CalculatePrice(t.Price, productCompositePrice, localDynamicExpressoParameters, decideClass);
                                        //}

                                        var _newComposite = new ProductCompositeResponseModel
                                        {
                                            BillingProductCompositeId = productCompositeList[t.ProductCompositeId].BillingProductComposite.Id,
                                            ResourceCompositeBundleIds = productCompositeList[t.ProductCompositeId].ResourceCompositeBundles?.Select(asd => asd.Id).ToList(),

                                            ProductComposite = productCompositeList[t.ProductCompositeId].ProductComposite,
                                            ProductSpecification = productCompositeList[t.ProductCompositeId].ProductSpecification,
                                            //Price = offerHelper.CalculatePrice(t.Price, productCompositePrice, localDynamicExpressoParameters, decideClass),

                                            Price = price,
                                            ProductCompositePrice = new ProductCompositePriceResponseModel
                                            {
                                                ProductCompositePriceId = productCompositePrice.ProductCompositePriceId,
                                                ListPrice = productCompositePrice.ListPrice,
                                                MinPrice = productCompositePrice.MinPrice,
                                                WithoutContractPrice = productCompositePrice.WithoutContractPrice,
                                                Version = productCompositePrice.Version
                                            },
                                            Discounts = discounts.ToList(),
                                            ExtraPrices = t.ExtraPrices?.ToDictionary(axc => axc.Key, axcc => axcc.Value),

                                            ProductComponents = productCompositeList[t.ProductCompositeId].ProductComponents
                                            ?.Select(k => new ProductComponentResponseModel
                                            {
                                                ProductComponent = new Common.Models.IdNameModel<Guid>
                                                {
                                                    Id = productComponentList.First(l => l.ProductComponentId == k).ProductComponentId,
                                                    Name = productComponentList.First(l => l.ProductComponentId == k).Name
                                                },

                                                ProductComponentPrices = productComponentList.First(l => l.ProductComponentId == k).ProductComponentPrices
                                                ?.Where(tk => tk.Version == entity.Version)
                                                .Select(l => new ProductComponentPriceResponseModel
                                                {
                                                    ProductComponentPriceId = l.ProductComponentPriceId,
                                                    Version = l.Version,
                                                    ListPrice = l.ListPrice,
                                                    MinPrice = l.MinPrice,
                                                    WithoutContractPrice = l.WithoutContractPrice
                                                }).ToList(),

                                                ProductComponentCharacteristicValues = productComponentList.First(l => l.ProductComponentId == k).ProductSpecificationCharacteristicValues
                                                ?.Select(l => new ProductComponentCharacteristicValueResponseModel
                                                {
                                                    ProductSpecificationCharacteristic = new Common.Models.IdNameModel<Guid>
                                                    {
                                                        Id = productSpecificationCharacteristicList.First(m => m.ProductSpecificationCharacteristicValues.Any(n => n.ProductSpecificationCharacteristicValueId == l)).ProductSpecificationCharacteristicId,
                                                        Name = productSpecificationCharacteristicList.First(m => m.ProductSpecificationCharacteristicValues.Any(n => n.ProductSpecificationCharacteristicValueId == l)).Name
                                                    },
                                                    ProductSpecificationCharacteristicDescription = productSpecificationCharacteristicList.First(m => m.ProductSpecificationCharacteristicValues.Any(n => n.ProductSpecificationCharacteristicValueId == l)).Description,
                                                    ProductSpecificationCharacteristicValueId = l,
                                                    Value = productSpecificationCharacteristicList.First(m => m.ProductSpecificationCharacteristicValues.Any(n => n.ProductSpecificationCharacteristicValueId == l)).ProductSpecificationCharacteristicValues.First(n => n.ProductSpecificationCharacteristicValueId == l).Value,
                                                    ValueType = productSpecificationCharacteristicList.First(m => m.ProductSpecificationCharacteristicValues.Any(n => n.ProductSpecificationCharacteristicValueId == l)).ProductSpecificationCharacteristicValues.First(n => n.ProductSpecificationCharacteristicValueId == l).ValueType,
                                                    Description = productSpecificationCharacteristicList.First(m => m.ProductSpecificationCharacteristicValues.Any(n => n.ProductSpecificationCharacteristicValueId == l)).ProductSpecificationCharacteristicValues.First(n => n.ProductSpecificationCharacteristicValueId == l).Description,
                                                    UnitOfMeasure = productSpecificationCharacteristicList.First(m => m.ProductSpecificationCharacteristicValues.Any(n => n.ProductSpecificationCharacteristicValueId == l)).ProductSpecificationCharacteristicValues.First(n => n.ProductSpecificationCharacteristicValueId == l).UnitOfMeasure,
                                                }).ToList()

                                            }).ToList()
                                        };
                                        if (isOldCompForTransference)
                                        {
                                            _newComposite.OldProductCompositeId = oldCompositeId;
                                        }
                                        offer.ProductComposites.Add(_newComposite);

                                    }
                                    catch (Exception ex)
                                    {
                                        //TODO: burada oluşan hata loglanarak kontrol edilmeli.
                                    }


                                }

                                if (entity.PriceCalculationClass != null)
                                {
                                    object[] prms = new[] { requestParemeters.SingleOrDefault(df => df.Property == "CompositeType")?.Value, requestParemeters.SingleOrDefault(df => df.Property == "TvAmount")?.Value };
                                    priceCalcClass.Calculate(localDynamicExpressoParameters, entity.PriceCalculationClass, offer, entity.Version, request.OfferTypeId, prms);

                                }

                                offer.ProductComposites = offer.ProductComposites.Select(x => { x.Discounts = x.Discounts!=null?x.Discounts.Where(k => k.Price != 0).ToList():null; x.TotalPrice = x.Discounts == null || x.Discounts.Count() == 0 ? x.Price : x.Price - x.Discounts.Where(k => k.IsPrimary.HasValue && k.IsPrimary.Value == true).Sum(y => y.Price); return x; }).ToList();
                                if (entity.BonusProductComposites != null && entity.BonusProductComposites.Any())
                                    offerHelper.FillBonusProductComposites(offer, entity.BonusProductComposites?.ToList(), entity.Version, productDic, productCompositeDic, productComponentDic, productSpecificationCharacteristicDic);

                                responseData.Add(offer);
                                if (request.OfferTypeId == OfferType.TransferInternet.Id)
                                {

                                    if (offer.ProductComposites.All(x => _offerTypeConstService.CurrentCompositePriceList.Select(x => x.ProductCompositeId).Contains(x.ProductComposite.Id) || _offerTypeConstService.CurrentCompositePriceList.Select(x => x.ProductCompositeId).Contains(x.OldProductCompositeId)))
                                    {
                                        foreach (var item in _offerTypeConstService.CurrentCompositePriceList)
                                        {
                                            var newProd = offer.ProductComposites.FirstOrDefault(x => x.ProductSpecification.Id == item.ProductSpecification.Id);
                                            if (newProd != null)
                                            {
                                                offer.ProductComposites.FirstOrDefault(x => x.ProductSpecification.Id == item.ProductSpecification.Id).Price = item.Price;

                                                if (offer.ProductComposites.FirstOrDefault(x => x.ProductSpecification.Id == item.ProductSpecification.Id).Discounts.Count() > 0 && item.Discounts != null && item.Discounts.Count() > 0)
                                                {
                                                    offer.ProductComposites.FirstOrDefault(x => x.ProductSpecification.Id == item.ProductSpecification.Id).Discounts.OrderByDescending(k => k.Price).FirstOrDefault().Price = item.Discounts.OrderByDescending(k => k.Price).FirstOrDefault().Price;
                                                    if (item.Discounts.Count() > 1 && newProd.Discounts.Count() < 2)
                                                    {
                                                        offer.ProductComposites.FirstOrDefault(x => x.ProductSpecification.Id == item.ProductSpecification.Id).Discounts.Add(new PriceModel<decimal>
                                                        {
                                                            Price = item.Discounts.OrderByDescending(k => k.Price).LastOrDefault().Price,
                                                            Duration = new Common.Models.DurationModel
                                                            {
                                                                Duration = 0,
                                                                DurationType = DurationTypeEnum.Month,
                                                            },
                                                            DurationStart = new Common.Models.DurationModel
                                                            {
                                                                Duration = 0,
                                                                DurationType = DurationTypeEnum.Month,
                                                            },
                                                            IsPrimary = false
                                                        });
                                                    }
                                                    offer.ProductComposites.FirstOrDefault(x => x.ProductSpecification.Id == item.ProductSpecification.Id).Discounts.OrderByDescending(k => k.Price).LastOrDefault().Price = item.Discounts.OrderByDescending(k => k.Price).LastOrDefault().Price;
                                                    if (item.Discounts.Count() == 1)
                                                    {
                                                        offer.ProductComposites.FirstOrDefault(x => x.ProductSpecification.Id == item.ProductSpecification.Id).Discounts.LastOrDefault().IsPrimary = true;
                                                    }
                                                    else
                                                    {
                                                        offer.ProductComposites.FirstOrDefault(x => x.ProductSpecification.Id == item.ProductSpecification.Id).Discounts.OrderByDescending(x => x.Price).FirstOrDefault().IsPrimary = true;
                                                        offer.ProductComposites.FirstOrDefault(x => x.ProductSpecification.Id == item.ProductSpecification.Id).Discounts.OrderByDescending(x => x.Price).LastOrDefault().IsPrimary = false;
                                                    }

                                                }
                                                else if (offer.ProductComposites.FirstOrDefault(x => x.ProductSpecification.Id == item.ProductSpecification.Id).Discounts.Count() > 0 && (item.Discounts == null || item.Discounts.Count() == 0))
                                                {
                                                    offer.ProductComposites.FirstOrDefault(x => x.ProductSpecification.Id == item.ProductSpecification.Id).Discounts = new List<PriceModel<decimal>>();
                                                }
                                                else if (item.Discounts != null && item.Discounts.Count() > 0){
                                                    offer.ProductComposites.FirstOrDefault(x => x.ProductSpecification.Id == item.ProductSpecification.Id).Discounts = new List<PriceModel<decimal>>();
                                                    offer.ProductComposites.FirstOrDefault(x => x.ProductSpecification.Id == item.ProductSpecification.Id).Discounts.Add(new PriceModel<decimal>
                                                    {
                                                        Price = item.Discounts.OrderByDescending(k => k.Price).FirstOrDefault().Price,
                                                        Duration = new Common.Models.DurationModel
                                                        {
                                                            Duration = 0,
                                                            DurationType = DurationTypeEnum.Month,
                                                        },
                                                        DurationStart = new Common.Models.DurationModel
                                                        {
                                                            Duration = 0,
                                                            DurationType = DurationTypeEnum.Month,
                                                        },
                                                        IsPrimary = true
                                                    });
                                                    if (item.Discounts.Count > 1)
                                                    {
                                                   
                                                        offer.ProductComposites.FirstOrDefault(x => x.ProductSpecification.Id == item.ProductSpecification.Id).Discounts.Add(new PriceModel<decimal>
                                                        {
                                                            Price = item.Discounts.OrderByDescending(k => k.Price).LastOrDefault().Price,
                                                            Duration = new Common.Models.DurationModel
                                                            {
                                                                Duration = 0,
                                                                DurationType = DurationTypeEnum.Month,
                                                            },
                                                            DurationStart = new Common.Models.DurationModel
                                                            {
                                                                Duration = 0,
                                                                DurationType = DurationTypeEnum.Month,
                                                            },
                                                            IsPrimary = false
                                                        });
                                                    }
                                                }


                                            }
                                        }
                                        offer.IsCurrentOffer = true;
                                    }
                                    else if (offer.ProductComposites.Where(x => x.ProductSpecification.Id != ProductSpecificationEnum.Internet.Id && x.ProductSpecification.Id != ProductSpecificationEnum.Naked.Id).All(x => _offerTypeConstService.CurrentCompositePriceList.Select(x => x.ProductCompositeId).Contains(x.ProductComposite.Id) || _offerTypeConstService.CurrentCompositePriceList.Select(x => x.ProductCompositeId).Contains(x.OldProductCompositeId)))
                                    {
                                        if (offer.ProductComposites.Any(x => x.ProductSpecification.Id == ProductSpecificationEnum.Internet.Id))
                                        {
                                            var currentInternetSpeed = localDynamicExpressoParameters.FirstOrDefault(k => k.Name == RuleParametersEnum.CurrentInternetBandwidth.Name).Value;
                                            var newInternetSpeed = offer.ProductComposites.FirstOrDefault(k => k.ProductSpecification.Id == ProductSpecificationEnum.Internet.Id).ProductComponents.SelectMany(k => k.ProductComponentCharacteristicValues).FirstOrDefault(k => k.ProductSpecificationCharacteristic.Name == ProductSpecificationCharacteristicEnum.Bandwidth.Name).Value;
                                            if (currentInternetSpeed == newInternetSpeed)
                                            {
                                                foreach (var item in _offerTypeConstService.CurrentCompositePriceList)
                                                {

                                                    var newProd = offer.ProductComposites.FirstOrDefault(x => x.ProductSpecification.Id == item.ProductSpecification.Id);
                                                    if (newProd != null)
                                                    {
                                                        offer.ProductComposites.FirstOrDefault(x => x.ProductSpecification.Id == item.ProductSpecification.Id).Price = item.Price;

                                                        if (offer.ProductComposites.FirstOrDefault(x => x.ProductSpecification.Id == item.ProductSpecification.Id).Discounts.Count() > 0 && item.Discounts != null && item.Discounts.Count() > 0)
                                                        {
                                                            offer.ProductComposites.FirstOrDefault(x => x.ProductSpecification.Id == item.ProductSpecification.Id).Discounts.OrderByDescending(k => k.Price).FirstOrDefault().Price = item.Discounts.OrderByDescending(k => k.Price).FirstOrDefault().Price;
                                                            if (item.Discounts.Count() > 1 && newProd.Discounts.Count() < 2)
                                                            {
                                                                offer.ProductComposites.FirstOrDefault(x => x.ProductSpecification.Id == item.ProductSpecification.Id).Discounts.Add(new PriceModel<decimal>
                                                                {
                                                                    Price = item.Discounts.OrderByDescending(k => k.Price).LastOrDefault().Price,
                                                                    Duration = new Common.Models.DurationModel
                                                                    {
                                                                        Duration = 0,
                                                                        DurationType = DurationTypeEnum.Month,
                                                                    },
                                                                    DurationStart = new Common.Models.DurationModel
                                                                    {
                                                                        Duration = 0,
                                                                        DurationType = DurationTypeEnum.Month,
                                                                    },
                                                                    IsPrimary = false
                                                                });
                                                            }
                                                            offer.ProductComposites.FirstOrDefault(x => x.ProductSpecification.Id == item.ProductSpecification.Id).Discounts.OrderByDescending(k => k.Price).LastOrDefault().Price = item.Discounts.OrderByDescending(k => k.Price).LastOrDefault().Price;
                                                            if (item.Discounts.Count() == 1)
                                                            {
                                                                offer.ProductComposites.FirstOrDefault(x => x.ProductSpecification.Id == item.ProductSpecification.Id).Discounts.LastOrDefault().IsPrimary = true;
                                                            }
                                                            else
                                                            {
                                                                offer.ProductComposites.FirstOrDefault(x => x.ProductSpecification.Id == item.ProductSpecification.Id).Discounts.OrderByDescending(x => x.Price).FirstOrDefault().IsPrimary = true;
                                                                offer.ProductComposites.FirstOrDefault(x => x.ProductSpecification.Id == item.ProductSpecification.Id).Discounts.OrderByDescending(x => x.Price).LastOrDefault().IsPrimary = false;
                                                            }

                                                        }
                                                        else if (offer.ProductComposites.FirstOrDefault(x => x.ProductSpecification.Id == item.ProductSpecification.Id).Discounts.Count() > 0 && (item.Discounts == null || item.Discounts.Count() == 0))
                                                        {
                                                            offer.ProductComposites.FirstOrDefault(x => x.ProductSpecification.Id == item.ProductSpecification.Id).Discounts = new List<PriceModel<decimal>>();
                                                        }
                                                        else if (item.Discounts != null && item.Discounts.Count() > 0){
                                                            offer.ProductComposites.FirstOrDefault(x => x.ProductSpecification.Id == item.ProductSpecification.Id).Discounts = new List<PriceModel<decimal>>();
                                                            offer.ProductComposites.FirstOrDefault(x => x.ProductSpecification.Id == item.ProductSpecification.Id).Discounts.Add(new PriceModel<decimal>
                                                            {
                                                                Price = item.Discounts.OrderByDescending(k => k.Price).FirstOrDefault().Price,
                                                                Duration = new Common.Models.DurationModel
                                                                {
                                                                    Duration = 0,
                                                                    DurationType = DurationTypeEnum.Month,
                                                                },
                                                                DurationStart = new Common.Models.DurationModel
                                                                {
                                                                    Duration = 0,
                                                                    DurationType = DurationTypeEnum.Month,
                                                                },
                                                                IsPrimary = true
                                                            });
                                                            if (item.Discounts.Count > 1)
                                                            {
                                                                offer.ProductComposites.FirstOrDefault(x => x.ProductSpecification.Id == item.ProductSpecification.Id).Discounts.Add(new PriceModel<decimal>
                                                                {
                                                                    Price = item.Discounts.OrderByDescending(k => k.Price).LastOrDefault().Price,
                                                                    Duration = new Common.Models.DurationModel
                                                                    {
                                                                        Duration = 0,
                                                                        DurationType = DurationTypeEnum.Month,
                                                                    },
                                                                    DurationStart = new Common.Models.DurationModel
                                                                    {
                                                                        Duration = 0,
                                                                        DurationType = DurationTypeEnum.Month,
                                                                    },
                                                                    IsPrimary = false
                                                                });
                                                            }
                                                        }


                                                    }
                                                }
                                            }
                                        }

                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Search Add {ProductOfferId}", entity.ProductOffer.Id);
                            }
                        }
                    }



                    if (request.CharacteristicList.Any(x => x.FirstOrDefault(k => k.Property == "Provider") != null && x.FirstOrDefault(k => k.Property == "Provider").Value == "TT"))
                    {
                        if (request.CharacteristicList.Any(x => x.FirstOrDefault(k => k.Property == "Provider") != null && x.FirstOrDefault(k => k.Property == "Provider").Value == "SOL"))
                        {

                        }
                        else
                        {
                            var withoutNakedSol = responseData.Where(x => !x.ProductComposites.Any(k => k.ProductComponents.Any(t => t.ProductComponentCharacteristicValues.Any(y => y.Value.ToLower() == "sol")))).ToList();
                            responseData = new ConcurrentBag<ProductOfferResponseModel>(withoutNakedSol);
                        }

                    }

                    if (request.ProductSpecifications.Any(x => x.Id == ProductSpecificationEnum.Internet.Id))
                    {
                        if (request.CharacteristicList.Any(x => x.FirstOrDefault(k => k.Property == "MaxSpeed") != null))
                        {
                            var speed = Convert.ToInt32(request.CharacteristicList.SelectMany(x => x).FirstOrDefault(k => k.Property == "MaxSpeed").Value);
                            if (speed > 12287)
                            {
                                var data = responseData.Where(x => !x.ProductComposites.Any(x => x.ProductComposite.Id == Guid.Parse("34651b79-fbd1-4b1f-b7c2-28f140f93d87") || x.ProductComposite.Id == Guid.Parse("eab7681d-2c9a-42be-b20f-4f081862b1d4") || x.ProductComposite.Id == Guid.Parse("f36d4e4c-57d3-4c4a-b359-90a2aa83b004")));
                                responseData = new ConcurrentBag<ProductOfferResponseModel>(data);
                            }
                        }
                    }
                    //bu offerdaki bu product in cevabi
                    ConcurrentDictionary<Tuple<Guid, Guid>, ProductOfferResponseModel> calculatedOffersDict = new ConcurrentDictionary<Tuple<Guid, Guid>, ProductOfferResponseModel>();

                    Parallel.ForEach(responseData/*, new ParallelOptions { MaxDegreeOfParallelism = 1 }*/, item =>
                                       {
                                           try
                                           {
                                               List<DynamicExpressoParameterModel> localDynamicExpressoParameters = new List<DynamicExpressoParameterModel>();

                                               foreach (var dyn in dynamicExpressoParameters)
                                               {
                                                   localDynamicExpressoParameters.Add(new DynamicExpressoParameterModel
                                                   {
                                                       Name = dyn.Name,
                                                       Type = dyn.Type,
                                                       Value = dyn.Value
                                                   });
                                               }
                                               if (offerDecoratorList.ContainsKey(item.ProductOffer.Id))
                                               {
                                                   var decoratorClass = offerDecoratorClassDict[item.ProductOffer.Id];
                                                   var foundOffer = decoratorClass.Calculate(calculatedOffersDict, request, item, item.Version, item.ProductOfferCatalog.Id, item.BundleProduct.Id, localDynamicExpressoParameters).GetAwaiter().GetResult();
                                               }

                                           }
                                           catch (Exception ex)
                                           {
                                               _logger.LogError(ex, "Search decoratorClass {@Request}", request);
                                           }
                                       });
                }


                //go+internet fix
                if (addGoproduct && _offerTypeConstService.CurrentGoComposite != null)
                {
                    //currentGoComposite type -> productresponse

                    foreach (var offer in responseData)
                    {
                        offer.ProductComposites.Add(new ProductCompositeResponseModel
                        {
                            BillingProductCompositeId = _offerTypeConstService.CurrentGoComposite.BillingProductCompositeId,
                            Price = _offerTypeConstService.CurrentGoComposite.Amount,
                            Discounts = new List<PriceModel<decimal>> { new PriceModel<decimal> { Duration = offer.Term, DurationStart = new Common.Models.DurationModel { Duration = 0, DurationType = DurationTypeEnum.Month }, Price = _offerTypeConstService.CurrentGoComposite.Discount } },
                            ProductComposite = new Common.Models.IdNameModel<Guid> { Id = _offerTypeConstService.CurrentGoComposite.ProductComposite.Id, Name = _offerTypeConstService.CurrentGoComposite.ProductComposite.Name },
                            ProductCompositePrice = new ProductCompositePriceResponseModel(),
                            ProductSpecification = _offerTypeConstService.CurrentGoComposite.ProductSpecification,
                            ProductComponents = _offerTypeConstService.CurrentGoComposite.ProductComponents.Select(x => new ProductComponentResponseModel
                            {
                                ProductComponent = x.ProductComponent,
                                ProductComponentCharacteristicValues = x.ProductComponentCharacteristicValues.Select(y => new ProductComponentCharacteristicValueResponseModel
                                {
                                    Description = y.Description,
                                    ProductSpecificationCharacteristic = new Common.Models.IdNameModel<Guid> { Id = Guid.Parse(y.ProductSpecificationCharacteristic.Id), Name = y.ProductSpecificationCharacteristic.Name },
                                    ProductSpecificationCharacteristicDescription = y.Description,
                                    ProductSpecificationCharacteristicValueId = y.ProductSpecificationCharacteristicValueId,
                                    UnitOfMeasure = y.UnitOfMeasure,
                                    Value = y.Value,
                                    ValueType = y.ValueType
                                }).ToList(),
                                ProductComponentPrices = new List<ProductComponentPriceResponseModel>()



                            }).ToList()

                        });
                    }
                }
                var offerResponseData = FixBundleOffersPayTvDiscountsPrices(responseData);




                var response = new ApiSearchResponse<ProductOfferResponseModel>
                {
                    IsSuccess = true,
                };
                response.Data.Items = offerResponseData;
                response.Data.PageSize = responseData?.Count ?? 0;
                response.Data.TotalCount = responseData?.Count ?? 0;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Search {@Request}", request);

                var response = new ApiSearchResponse<ProductOfferResponseModel>
                {
                    IsSuccess = false,
                    Message = ex.Message + " - " + ex.InnerException?.Message

                };
                return Ok(response);
            }
        }

        [HttpPost(ProductOfferingApiMethodConst.ProductOffer.SearchForGo)]
        [ProducesResponseType(200, Type = typeof(ApiSearchResponse<ProductOfferResponseModel>))]
        public async Task<IActionResult> SearchForGo([FromBody] ProductOfferRequestModel request)
        {

            try
            {
                //_logger.LogInformation(Newtonsoft.Json.JsonConvert.SerializeObject(request));
                //request.SalesChannel = Guid.Parse("3e6a99ca-6c8d-4037-8716-ace57980e5fc");
                var provider = _cacheProvider.GetCachingProvider("DefaultInMemory");
                if (!await provider.ExistsAsync("ProductOfferProductMapCache"))
                {
                    //set cache
                    await _cachingHelper.ResetCacheOfferSearch();
                }
                var productOfferProductMapDic = (await provider.GetAsync<Dictionary<Guid, ProductOfferProductMap>>("ProductOfferProductMapCache")).Value.Values;
                var productDic = (await provider.GetAsync<Dictionary<Guid, Product>>("ProductCache")).Value.Values;
                var productOfferDic = (await provider.GetAsync<Dictionary<Guid, ProductOffer>>("ProductOfferCache")).Value.Values;
                var productOfferCatalogDic = (await provider.GetAsync<Dictionary<Guid, ProductOfferCatalog>>("ProductOfferCatalogCache")).Value.Values;

                var productCompositeDic = (await provider.GetAsync<Dictionary<Guid, ProductComposite>>("ProductCompositeCache")).Value.Values;
                var productComponentDic = (await provider.GetAsync<Dictionary<Guid, ProductComponent>>("ProductComponentCache")).Value.Values;
                var productOfferRuleDic = (await provider.GetAsync<Dictionary<Guid, ProductOfferRule>>("ProductOfferRuleCache")).Value;
                var productSpecificationCharacteristicDic = (await provider.GetAsync<Dictionary<Guid, ProductSpecificationCharacteristic>>("ProductSpecificationCharacteristicCache")).Value.Values;
                var requestParameterDic = (await provider.GetAsync<Dictionary<string, RequestParameter>>("RequestParameterCache")).Value.Values;
                var campaignDetailsDic = (await provider.GetAsync<Dictionary<Guid, CampaignDetails>>("CampaignDetailsCache")).Value;
                var productCompositeEquivalentDic = (await provider.GetAsync<Lookup<Guid, ProductCompositeEquivalents>>("ProductCompositeEquivalentsCache")).Value;

                var characteristicList = await CharacteristicsParametersSplitterHelper.SplitCharacteristics(request.CharacteristicList, _mongoRepository);
                var requestParemeters = await CharacteristicsParametersSplitterHelper.SplitParameters(request.CharacteristicList, _mongoRepository);
                var productSpecifications = CharacteristicsParametersSplitterHelper.SplitSpecifications(request.ProductSpecifications, request.CharacteristicList, request.OfferTypeId);

                var versions = VersionHelper.Find(request.OfferTypeId, productSpecifications);


                //sadece aktif productlari aktifler
                productDic = productDic.Where(x => !x.Disabled).ToDictionary(x => x.ProductId).Values;

                //offer type anlamak için lazım.
                _offerTypeConstService.RequestedProductSpecifications = request.ProductSpecifications;

                var grupList = UserGroupIds;
                if (UserId == Guid.Parse("beaee219-beb9-4617-93bf-68e71d43da61"))
                {
                    grupList = new List<int> { 1, 70, 60, 84, 607, 610, 609, 85, 51, 72 };
                }
                var query = from offerMap in productOfferProductMapDic
                            join product in productDic on offerMap.ProductId equals product.ProductId
                            join offer in productOfferDic on offerMap.ProductOfferId equals offer.ProductOfferId
                            join catalog in productOfferCatalogDic on offer.ProductOfferCatalogId equals catalog.ProductOfferCatalogId
                            where catalog.SalesChannels.Any(x => x == request.SalesChannel)
                              && offerMap.StartDate <= DateTime.Now
                              && offerMap.EndDate >= DateTime.Now
                              && product.StartDate <= DateTime.Now
                              && product.EndDate >= DateTime.Now
                              && catalog.StartDate <= DateTime.Now
                              && catalog.EndDate >= DateTime.Now
                              && offer.StartDate <= DateTime.Now
                              && offer.EndDate >= DateTime.Now
                            //&& offer.OfferType.Id == OfferType.ReverseCrossFromInternetToTv.Id
                            //&& offerMap.ProductOfferId == Guid.Parse("274cf35e-0e71-4f72-8dc7-9e6564172cf0")
                            //&& offerMap.ProductId == Guid.Parse("f4ace640-a9e7-461f-af2a-45c7bc3c9535")
                            //&& new List<Guid> { Guid.Parse("a7791360-b819-40be-8fdc-5c36b157cade"), Guid.Parse("d8cb29ab-bd99-458a-b772-d187f2ae4ead"), Guid.Parse("0406397c-e88c-4d0d-b496-a32ee0bfb43e") }.Contains( offer.ProductOfferCatalogId)
                            //&& offerMap.ProductComposites.Any(x=>x.ProductCompositeId == Guid.Parse("09ce76fc-b3c8-48c5-b69d-9167fe8b7797"))
                            select new
                            {
                                offerMap.ProductOfferProductMapId,
                                offerMap.Version,
                                offerMap.Rules,
                                Product = new Common.Models.IdNameModel<Guid> { Id = product.ProductId, Name = product.Name },
                                ProductOffer = new Common.Models.IdNameModel<Guid> { Id = offer.ProductOfferId, Name = offer.Name },
                                ProductOfferCatalog = new Common.Models.IdNameModel<Guid> { Id = catalog.ProductOfferCatalogId, Name = catalog.Name },
                                OfferRoutingType = offer.OfferRoutingType,
                                Term = new Common.Models.DurationModel
                                {
                                    Duration = offer.Term.Duration,
                                    DurationType = offer.Term.DurationType,
                                },
                                OTFs = offer.OTFs,
                                ProductComposites = offerMap.ProductComposites,
                                ProductOfferRules = offer.ProductOfferRules,
                                offer.PaymentType,
                                offer.FeeType,
                                offer.InstallmentsCount,
                                offer.StbOwnerOnly,
                                offerMap.BonusProductComposites,
                                CampaignDetailId = offer.CampaignDetailId ?? Guid.Empty,
                                offer.PriceCalculationClass,
                                offer.OfferType,
                                offer.ProductOfferDecorators
                            };

                var searchResult = query.ToList();
                //searchResult = searchResult.Where(x => x.ProductOffer.Name.Contains("2301004") && x.Product.Name.ToLower().Contains("fiber vdsl 24mb ekadar (fttb) - limitsiz") && x.Product.Name.ToLower().Contains("yalin")).ToList();

                var responseData = new ConcurrentBag<ProductOfferResponseModel>();
                if (searchResult != null && searchResult.Any())
                {
                    var productCompositeIds = searchResult.SelectMany(x => x.ProductComposites).Select(x => x.ProductCompositeId).Distinct().ToList();

                    var productCompositeList = productCompositeDic.Where(x => productCompositeIds.Contains(x.ProductCompositeId))
                                                .Select(composite => new
                                                {
                                                    ProductComposite = new Common.Models.IdNameModel<Guid> { Id = composite.ProductCompositeId, Name = composite.Name },
                                                    composite.ProductSpecification,
                                                    composite.ProductComponents,
                                                    composite.BillingProductComposite,
                                                    composite.ResourceCompositeBundles,
                                                    composite.ProductCompositePrices,
                                                    composite.Neighborhoods
                                                }).ToDictionary(x => x.ProductComposite.Id);


                    var productComponentIds = productCompositeList.SelectMany(x => x.Value.ProductComponents).Distinct().ToList();
                    var productComponentList = productComponentDic.Where(x => productComponentIds.Contains(x.ProductComponentId)).ToList();


                    var productSpecificationCharacteristicValueIds = productComponentList.SelectMany(x => x.ProductSpecificationCharacteristicValues).Distinct().ToList();
                    var productSpecificationCharacteristicList = productSpecificationCharacteristicDic.Where(x => x.ProductSpecificationCharacteristicValues.Any(t => productSpecificationCharacteristicValueIds.Contains(t.ProductSpecificationCharacteristicValueId))).ToList();


                    var dynamicExpressoParameters = CharacteristicsParametersSplitterHelper.SplitDynamicExpressoParametersFromRequestModel(request.CharacteristicList);




                    var defaultRequestParameter = requestParameterDic.First(x => x.Name == "Default");
                    var decideClass = ActivatorUtilities.CreateInstance(_provider, Type.GetType(defaultRequestParameter.CalculationClass)) as IGetParameters;








                    var priceClassDict = new Dictionary<string, IPriceCalculator>();

                    foreach (var className in searchResult.Where(x => x.PriceCalculationClass != null).Select(x => x.PriceCalculationClass.ClassName).Distinct())
                    {
                        var priceCalcClass = ActivatorUtilities.CreateInstance(_provider, Type.GetType(className)) as IPriceCalculator;

                        priceClassDict.Add(className, priceCalcClass);
                    }


                    var offerDecoratorList = searchResult.Where(x => x.ProductOfferDecorators != null && x.ProductOfferDecorators.Any(y => y.Version == x.Version)).GroupBy(x => x.ProductOffer.Id).Select(x => new { x.First().ProductOfferDecorators.Single(y => y.Version == x.First().Version).ClassName, ProductOfferId = x.Key }).ToDictionary(x => x.ProductOfferId);

                    var offerDecoratorClassDict = new Dictionary<Guid, IOfferDecorator>();

                    foreach (var x in offerDecoratorList)
                    {
                        var decoratorClass = ActivatorUtilities.CreateInstance(_provider, Type.GetType(x.Value.ClassName)) as IOfferDecorator;

                        offerDecoratorClassDict.Add(x.Key, decoratorClass);
                    }

                    var offerHelper = new OfferHelper(_logger, _offerTypeConstService, _provider, _cacheProvider);



                    foreach (var entity in searchResult)

                    {
                        List<DynamicExpressoParameterModel> localDynamicExpressoParameters = new List<DynamicExpressoParameterModel>();

                        foreach (var item in dynamicExpressoParameters)
                        {
                            localDynamicExpressoParameters.Add(new DynamicExpressoParameterModel
                            {
                                Name = item.Name,
                                Type = item.Type,
                                Value = item.Value
                            });
                        }



                        try
                        {
                            IPriceCalculator priceCalcClass = null;
                            if (entity.PriceCalculationClass != null)
                            {
                                priceCalcClass = priceClassDict[entity.PriceCalculationClass.ClassName];
                            }

                            var offer = new ProductOfferResponseModel
                            {
                                BundleProduct = entity.Product,
                                ProductOffer = entity.ProductOffer,
                                ProductOfferCatalog = entity.ProductOfferCatalog,
                                OfferRoutingType = entity.OfferRoutingType,
                                Term = entity.Term,
                                PaymentType = entity.PaymentType,
                                OfferType = entity.OfferType,
                                InstallmentsCount = entity.InstallmentsCount,
                                StbOwnerOnly = entity.StbOwnerOnly,
                                FeeType = entity.FeeType,
                                Version = entity.Version
                            };
                            if (entity.CampaignDetailId != Guid.Empty)
                            {
                                var campaignDetail = campaignDetailsDic[entity.CampaignDetailId];
                                offer.CampaignDetail = new CampaignDetailResponseModel
                                {
                                    CampaignDetailId = campaignDetail.CampaignDetailId,
                                    Name = campaignDetail.Name,
                                    Form = campaignDetail.Form
                                };
                            }

                            offer.OTFs = entity.OTFs
                                    ?.Select(t => new OTFModel<decimal>
                                    {
                                        Default = t.Default,
                                        OTFId = t.OTFId,
                                        OTFType = t.OTFType,
                                        Price = new PriceModel<decimal>
                                        {
                                            Price = decimal.Parse(t.Price.Price),
                                            Duration = new Common.Models.DurationModel
                                            {
                                                Duration = t.Price.Duration.Duration,
                                                DurationType = t.Price.Duration.DurationType,
                                            },
                                            DurationStart = new Common.Models.DurationModel
                                            {
                                                Duration = t.Price.DurationStart.Duration,
                                                DurationType = t.Price.DurationStart.DurationType,
                                            }
                                        }
                                    }).ToList();

                            offer.ProductOfferRules = entity.ProductOfferRules
                                    ?.Select(t => new Common.Models.IdNameModel<Guid>
                                    {
                                        Id = t,
                                        Name = productOfferRuleDic[t].Formula
                                    }).ToList();

                            offer.ProductComposites = new List<ProductCompositeResponseModel>();


                            foreach (var t in entity.ProductComposites)
                            {
                                try
                                {
                                    var productCompositePrice = productCompositeList[t.ProductCompositeId].ProductCompositePrices.Single(tk => tk.Version == entity.Version);

                                    localDynamicExpressoParameters = decideClass.Calculate(localDynamicExpressoParameters, defaultRequestParameter.RuleParameters, request.OfferTypeId, entity.Version, productCompositePrice).GetAwaiter().GetResult();


                                    var discounts = t.Discounts?.Select(k => new PriceModel<decimal>
                                    {
                                        Price = offerHelper.CalculatePrice(k.Price, productCompositePrice, localDynamicExpressoParameters, decideClass),
                                        Duration = new Common.Models.DurationModel
                                        {
                                            Duration = k.Duration.Duration,
                                            DurationType = k.Duration.DurationType,
                                        },
                                        DurationStart = new Common.Models.DurationModel
                                        {
                                            Duration = k.DurationStart.Duration,
                                            DurationType = k.DurationStart.DurationType,
                                        },
                                        IsPrimary = k.IsPrimary == true ? true : false,
                                    });

                                    if (t.Discounts != null && t.Discounts.Any(k => !k.IsPrimary.HasValue))
                                    {
                                        if (t.Discounts.Count() > 1)
                                        {
                                            t.Discounts.OrderByDescending(k => k.Duration.Duration).FirstOrDefault().IsPrimary = true;
                                        }
                                        else if (t.Discounts.Count() == 1)
                                        {
                                            t.Discounts.FirstOrDefault().IsPrimary = true;
                                        }
                                    }

                                    decimal price = offerHelper.CalculatePrice(t.Price, productCompositePrice, localDynamicExpressoParameters, decideClass);

                                    var _newComposite = new ProductCompositeResponseModel
                                    {
                                        BillingProductCompositeId = productCompositeList[t.ProductCompositeId].BillingProductComposite.Id,
                                        ResourceCompositeBundleIds = productCompositeList[t.ProductCompositeId].ResourceCompositeBundles?.Select(asd => asd.Id).ToList(),

                                        ProductComposite = productCompositeList[t.ProductCompositeId].ProductComposite,
                                        ProductSpecification = productCompositeList[t.ProductCompositeId].ProductSpecification,
                                        Price = price,
                                        ProductCompositePrice = new ProductCompositePriceResponseModel
                                        {
                                            ProductCompositePriceId = productCompositePrice.ProductCompositePriceId,
                                            ListPrice = productCompositePrice.ListPrice,
                                            MinPrice = productCompositePrice.MinPrice,
                                            WithoutContractPrice = productCompositePrice.WithoutContractPrice,
                                            Version = productCompositePrice.Version
                                        },
                                        Discounts = discounts.ToList(),
                                        ExtraPrices = t.ExtraPrices?.ToDictionary(axc => axc.Key, axcc => axcc.Value),

                                        ProductComponents = productCompositeList[t.ProductCompositeId].ProductComponents
                                            ?.Select(k => new ProductComponentResponseModel
                                            {
                                                ProductComponent = new Common.Models.IdNameModel<Guid>
                                                {
                                                    Id = productComponentList.First(l => l.ProductComponentId == k).ProductComponentId,
                                                    Name = productComponentList.First(l => l.ProductComponentId == k).Name
                                                },

                                                ProductComponentPrices = productComponentList.First(l => l.ProductComponentId == k).ProductComponentPrices
                                                ?.Where(tk => tk.Version == entity.Version)
                                                .Select(l => new ProductComponentPriceResponseModel
                                                {
                                                    ProductComponentPriceId = l.ProductComponentPriceId,
                                                    Version = l.Version,
                                                    ListPrice = l.ListPrice,
                                                    MinPrice = l.MinPrice,
                                                    WithoutContractPrice = l.WithoutContractPrice
                                                }).ToList(),

                                                ProductComponentCharacteristicValues = productComponentList.First(l => l.ProductComponentId == k).ProductSpecificationCharacteristicValues
                                                ?.Select(l => new ProductComponentCharacteristicValueResponseModel
                                                {
                                                    ProductSpecificationCharacteristic = new Common.Models.IdNameModel<Guid>
                                                    {
                                                        Id = productSpecificationCharacteristicList.First(m => m.ProductSpecificationCharacteristicValues.Any(n => n.ProductSpecificationCharacteristicValueId == l)).ProductSpecificationCharacteristicId,
                                                        Name = productSpecificationCharacteristicList.First(m => m.ProductSpecificationCharacteristicValues.Any(n => n.ProductSpecificationCharacteristicValueId == l)).Name
                                                    },
                                                    ProductSpecificationCharacteristicDescription = productSpecificationCharacteristicList.First(m => m.ProductSpecificationCharacteristicValues.Any(n => n.ProductSpecificationCharacteristicValueId == l)).Description,
                                                    ProductSpecificationCharacteristicValueId = l,
                                                    Value = productSpecificationCharacteristicList.First(m => m.ProductSpecificationCharacteristicValues.Any(n => n.ProductSpecificationCharacteristicValueId == l)).ProductSpecificationCharacteristicValues.First(n => n.ProductSpecificationCharacteristicValueId == l).Value,
                                                    ValueType = productSpecificationCharacteristicList.First(m => m.ProductSpecificationCharacteristicValues.Any(n => n.ProductSpecificationCharacteristicValueId == l)).ProductSpecificationCharacteristicValues.First(n => n.ProductSpecificationCharacteristicValueId == l).ValueType,
                                                    Description = productSpecificationCharacteristicList.First(m => m.ProductSpecificationCharacteristicValues.Any(n => n.ProductSpecificationCharacteristicValueId == l)).ProductSpecificationCharacteristicValues.First(n => n.ProductSpecificationCharacteristicValueId == l).Description,
                                                    UnitOfMeasure = productSpecificationCharacteristicList.First(m => m.ProductSpecificationCharacteristicValues.Any(n => n.ProductSpecificationCharacteristicValueId == l)).ProductSpecificationCharacteristicValues.First(n => n.ProductSpecificationCharacteristicValueId == l).UnitOfMeasure,
                                                }).ToList()

                                            }).ToList()
                                    };

                                    offer.ProductComposites.Add(_newComposite);

                                }
                                catch (Exception ex)
                                {
                                    //TODO: burada oluşan hata loglanarak kontrol edilmeli.
                                }
                            }

                            if (entity.PriceCalculationClass != null)
                            {
                                object[] prms = new[] { requestParemeters.SingleOrDefault(df => df.Property == "CompositeType")?.Value, requestParemeters.SingleOrDefault(df => df.Property == "TvAmount")?.Value };
                                priceCalcClass.Calculate(localDynamicExpressoParameters, entity.PriceCalculationClass, offer, entity.Version, request.OfferTypeId, prms);

                            }

                            offer.ProductComposites = offer.ProductComposites.Select(x => { x.TotalPrice = x.Discounts == null || x.Discounts.Count() == 0 ? x.Price : x.Price - x.Discounts.Where(k => k.IsPrimary.HasValue && k.IsPrimary.Value == true).Sum(y => y.Price); return x; }).ToList();
                            if (entity.BonusProductComposites != null && entity.BonusProductComposites.Any())
                                offerHelper.FillBonusProductComposites(offer, entity.BonusProductComposites?.ToList(), entity.Version, productDic, productCompositeDic, productComponentDic, productSpecificationCharacteristicDic);

                            responseData.Add(offer);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Search Add {ProductOfferId}", entity.ProductOffer.Id);
                        }

                    }




                    //bu offerdaki bu product in cevabi
                    ConcurrentDictionary<Tuple<Guid, Guid>, ProductOfferResponseModel> calculatedOffersDict = new ConcurrentDictionary<Tuple<Guid, Guid>, ProductOfferResponseModel>();

                    Parallel.ForEach(responseData/*, new ParallelOptions { MaxDegreeOfParallelism = 1 }*/, item =>
                    {
                        try
                        {
                            List<DynamicExpressoParameterModel> localDynamicExpressoParameters = new List<DynamicExpressoParameterModel>();

                            foreach (var dyn in dynamicExpressoParameters)
                            {
                                localDynamicExpressoParameters.Add(new DynamicExpressoParameterModel
                                {
                                    Name = dyn.Name,
                                    Type = dyn.Type,
                                    Value = dyn.Value
                                });
                            }
                            if (offerDecoratorList.ContainsKey(item.ProductOffer.Id))
                            {
                                var decoratorClass = offerDecoratorClassDict[item.ProductOffer.Id];
                                var foundOffer = decoratorClass.Calculate(calculatedOffersDict, request, item, item.Version, item.ProductOfferCatalog.Id, item.BundleProduct.Id, localDynamicExpressoParameters).GetAwaiter().GetResult();
                            }

                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Search decoratorClass {@Request}", request);
                        }
                    });
                }



                var offerResponseData = FixBundleOffersPayTvDiscountsPrices(responseData);




                var response = new ApiSearchResponse<ProductOfferResponseModel>
                {
                    IsSuccess = true,
                };
                response.Data.Items = offerResponseData;
                response.Data.PageSize = responseData?.Count ?? 0;
                response.Data.TotalCount = responseData?.Count ?? 0;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Search {@Request}", request);

                var response = new ApiSearchResponse<ProductOfferResponseModel>
                {
                    IsSuccess = false,
                    Message = ex.Message + " - " + ex.InnerException?.Message

                };
                return Ok(response);
            }
        }
        [HttpPost(ProductOfferingApiMethodConst.ProductOffer.SearchForConversion)]
        [ProducesResponseType(200, Type = typeof(ApiSearchResponse<ProductOfferResponseModel>))]
        public async Task<IActionResult> SearchForConversion([FromBody] ProductOfferRequestModelConversion request)
        {

            try
            {
                //_logger.LogInformation(Newtonsoft.Json.JsonConvert.SerializeObject(request));

                var provider = _cacheProvider.GetCachingProvider("DefaultInMemory");
                if (!await provider.ExistsAsync("ProductOfferProductMapCache"))
                {
                    //set cache
                    await _cachingHelper.ResetCacheOfferSearch();
                }
                var productOfferProductMapDic = (await provider.GetAsync<Dictionary<Guid, ProductOfferProductMap>>("ProductOfferProductMapCache")).Value.Values;
                var productDic = (await provider.GetAsync<Dictionary<Guid, Product>>("ProductCache")).Value.Values;
                var productOfferDic = (await provider.GetAsync<Dictionary<Guid, ProductOffer>>("ProductOfferCache")).Value.Values;
                var productOfferCatalogDic = (await provider.GetAsync<Dictionary<Guid, ProductOfferCatalog>>("ProductOfferCatalogCache")).Value.Values;

                var productCompositeDic = (await provider.GetAsync<Dictionary<Guid, ProductComposite>>("ProductCompositeCache")).Value.Values;
                var productComponentDic = (await provider.GetAsync<Dictionary<Guid, ProductComponent>>("ProductComponentCache")).Value.Values;
                var productOfferRuleDic = (await provider.GetAsync<Dictionary<Guid, ProductOfferRule>>("ProductOfferRuleCache")).Value;
                var productSpecificationCharacteristicDic = (await provider.GetAsync<Dictionary<Guid, ProductSpecificationCharacteristic>>("ProductSpecificationCharacteristicCache")).Value.Values;
                var requestParameterDic = (await provider.GetAsync<Dictionary<string, RequestParameter>>("RequestParameterCache")).Value.Values;
                var campaignDetailsDic = (await provider.GetAsync<Dictionary<Guid, CampaignDetails>>("CampaignDetailsCache")).Value;
                var productCompositeEquivalentDic = (await provider.GetAsync<Lookup<Guid, ProductCompositeEquivalents>>("ProductCompositeEquivalentsCache")).Value;

                var characteristicList = await CharacteristicsParametersSplitterHelper.SplitCharacteristics(request.CharacteristicList, _mongoRepository);
                var requestParemeters = await CharacteristicsParametersSplitterHelper.SplitParameters(request.CharacteristicList, _mongoRepository);
                var productSpecifications = CharacteristicsParametersSplitterHelper.SplitSpecifications(request.ProductSpecifications, request.CharacteristicList, request.OfferTypeId);

                var versions = VersionHelper.Find(request.OfferTypeId, productSpecifications);


                //sadece aktif productlari aktifler
                productDic = productDic.Where(x => !x.Disabled).ToDictionary(x => x.ProductId).Values;

                //offer type anlamak için lazım.
                _offerTypeConstService.RequestedProductSpecifications = request.ProductSpecifications;
                bool addGoproduct = false;

                //go+internet fix, paytv yoksa ve go varsa intenettle beraber, sadece internet gibi gor
                if (_offerTypeConstService.RequestedProductSpecifications.Contains(ProductSpecificationEnum.Internet) && !_offerTypeConstService.RequestedProductSpecifications.Contains(ProductSpecificationEnum.PayTv)
                    && _offerTypeConstService.RequestedProductSpecifications.Contains(ProductSpecificationEnum.Go) && (request.OfferTypeId == OfferType.SolSwap.Id || request.OfferTypeId == OfferType.Raise.Id || request.OfferTypeId == OfferType.Renew.Id || request.OfferTypeId == OfferType.Upgrade.Id))
                {
                    _offerTypeConstService.RequestedProductSpecifications.Remove(ProductSpecificationEnum.Go);
                    addGoproduct = true;
                }
                var grupList = UserGroupIds;
                if (UserId == Guid.Parse("beaee219-beb9-4617-93bf-68e71d43da61"))
                {
                    grupList = new List<int> { 1, 70, 60, 84, 607, 610, 609, 85, 51, 72 };
                }
                var offerId = request.ProductSpecifications.Select(x => x.Id).Contains(ProductSpecificationEnum.PayTv.Id) ? Guid.Parse("ee92d31e-6b02-4974-b73e-8e6ff2980d99") : Guid.Parse("ab49cd8d-1526-4c98-8ac7-efdb3184ae4f");
                var query = from offerMap in productOfferProductMapDic
                            join product in productDic on offerMap.ProductId equals product.ProductId
                            join offer in productOfferDic on offerMap.ProductOfferId equals offer.ProductOfferId
                            join catalog in productOfferCatalogDic on offer.ProductOfferCatalogId equals catalog.ProductOfferCatalogId
                            where catalog.SalesChannels.Any(x => x == request.SalesChannel)
                              && catalog.OfferForms.Contains(request.OfferFormId)
                              && offer.ProductOfferId == offerId
                            //&& offer.OfferType.Id == OfferType.ReverseCrossFromInternetToTv.Id
                            //&& offerMap.ProductOfferId == Guid.Parse("274cf35e-0e71-4f72-8dc7-9e6564172cf0")
                            //&& offerMap.ProductId == Guid.Parse("f4ace640-a9e7-461f-af2a-45c7bc3c9535")
                            //&& new List<Guid> { Guid.Parse("a7791360-b819-40be-8fdc-5c36b157cade"), Guid.Parse("d8cb29ab-bd99-458a-b772-d187f2ae4ead"), Guid.Parse("0406397c-e88c-4d0d-b496-a32ee0bfb43e") }.Contains( offer.ProductOfferCatalogId)
                            //&& offerMap.ProductComposites.Any(x=>x.ProductCompositeId == Guid.Parse("09ce76fc-b3c8-48c5-b69d-9167fe8b7797"))
                            select new
                            {
                                offerMap.ProductOfferProductMapId,
                                offerMap.Version,
                                offerMap.Rules,
                                Product = new Common.Models.IdNameModel<Guid> { Id = product.ProductId, Name = product.Name },
                                ProductOffer = new Common.Models.IdNameModel<Guid> { Id = offer.ProductOfferId, Name = offer.Name },
                                ProductOfferCatalog = new Common.Models.IdNameModel<Guid> { Id = catalog.ProductOfferCatalogId, Name = catalog.Name },
                                OfferRoutingType = offer.OfferRoutingType,
                                Term = new Common.Models.DurationModel
                                {
                                    Duration = offer.Term.Duration,
                                    DurationType = offer.Term.DurationType,
                                },
                                OTFs = offer.OTFs,
                                ProductComposites = offerMap.ProductComposites,
                                ProductOfferRules = offer.ProductOfferRules,
                                offer.PaymentType,
                                offer.FeeType,
                                offer.InstallmentsCount,
                                offer.StbOwnerOnly,
                                offerMap.BonusProductComposites,
                                CampaignDetailId = offer.CampaignDetailId ?? Guid.Empty,
                                offer.PriceCalculationClass,
                                offer.OfferType,
                                offer.ProductOfferDecorators
                            };


                var searchResult = query.ToList();

                if (productSpecifications != null && productSpecifications.Any())
                {
                    var requestProductSpecificationIds = productSpecifications.Select(a => a.Id).ToList();
                    var productCompositeFilterIds = productCompositeDic.Where(x => requestProductSpecificationIds.Contains(x.ProductSpecification.Id)).Select(x => x.ProductCompositeId).ToList();
                    searchResult = searchResult.Where(x => x.ProductComposites.Count == productSpecifications.Count).ToList();
                    //searchResult = searchResult.Where(x => x.ProductComposites.All(t => productCompositeFilterIds.Contains(t.ProductCompositeId))).ToList();
                }

                var responseData = new ConcurrentBag<ProductOfferResponseModel>();
                if (searchResult != null && searchResult.Any())
                {
                    var productCompositeIds = searchResult.SelectMany(x => x.ProductComposites).Select(x => x.ProductCompositeId).Distinct().ToList();

                    var productCompositeList = productCompositeDic.Where(x => productCompositeIds.Contains(x.ProductCompositeId))
                                                .Select(composite => new
                                                {
                                                    ProductComposite = new Common.Models.IdNameModel<Guid> { Id = composite.ProductCompositeId, Name = composite.Name },
                                                    composite.ProductSpecification,
                                                    composite.ProductComponents,
                                                    composite.BillingProductComposite,
                                                    composite.ResourceCompositeBundles,
                                                    composite.ProductCompositePrices,
                                                    composite.Neighborhoods
                                                }).ToDictionary(x => x.ProductComposite.Id);


                    var productComponentIds = productCompositeList.SelectMany(x => x.Value.ProductComponents).Distinct().ToList();
                    var productComponentList = productComponentDic.Where(x => productComponentIds.Contains(x.ProductComponentId)).ToList();

                    var productSpecificationCharacteristicValueIds = productComponentList.SelectMany(x => x.ProductSpecificationCharacteristicValues).Distinct().ToList();
                    var productSpecificationCharacteristicList = productSpecificationCharacteristicDic.Where(x => x.ProductSpecificationCharacteristicValues.Any(t => productSpecificationCharacteristicValueIds.Contains(t.ProductSpecificationCharacteristicValueId))).ToList();


                    foreach (var item in request.ProductCompositeList)
                    {
                        var compositeId = productCompositeEquivalentDic[item]?.SingleOrDefault(x => versions.Contains(x.Version))?.NewProductCompositeId ?? item;
                        if (compositeId != Guid.Parse("a83dabaa-224d-4fbf-bb68-756f4bdd1342"))
                        {
                            searchResult = searchResult.Where(x => x.ProductComposites.Select(k => k.ProductCompositeId).Contains(compositeId)).ToList();
                        }

                    }

                    var dynamicExpressoParameters = CharacteristicsParametersSplitterHelper.SplitDynamicExpressoParametersFromRequestModel(request.CharacteristicList);
                    if (productSpecifications.Any() && productSpecifications.Any(x => x.Id == ProductSpecificationEnum.PayTv.Id))
                    {
                        var transmissionFee = await _mongoRepository.GetAsync<Fee>(x => x.Name == "İletim Bedeli" && (x.ValidFrom <= DateTime.Now && x.ValidThru >= DateTime.Now));
                        if (transmissionFee != null)
                        {
                            dynamicExpressoParameters.Add(new DynamicExpressoParameterModel
                            {
                                Name = "TransmissionFee",
                                Type = "System.Decimal",
                                Value = transmissionFee.Price.ToString()
                            });
                        }
                    }

                    if (requestParemeters != null && requestParemeters.Any())
                    {
                        var properties = requestParemeters.Select(a => a.Property);
                        var requestParameters = requestParameterDic.Where(x => properties.Contains(x.Name)).ToList();
                        if (requestParameters != null && requestParameters.Any())
                        {
                            foreach (var requestParameter in requestParameters)
                            {
                                var decideClass1 = ActivatorUtilities.CreateInstance(_provider, Type.GetType(requestParameter.CalculationClass)) as IGetParameters;
                                foreach (var version in versions)
                                {
                                    dynamicExpressoParameters = (await decideClass1.Calculate(dynamicExpressoParameters, requestParameter.RuleParameters, request.OfferTypeId, version, requestParemeters.First(df => df.Property == requestParameter.Name).Value));
                                }
                            }
                        }
                    }




                    var defaultRequestParameter = requestParameterDic.First(x => x.Name == "Default");
                    var decideClass = ActivatorUtilities.CreateInstance(_provider, Type.GetType(defaultRequestParameter.CalculationClass)) as IGetParameters;


                    //must show same productComposite with cross offers when current product contains paytv 
                    //todooo
                    if (_offerTypeConstService.CurrentProductSpecifications.Any(aa => aa.Id == ProductSpecificationEnum.PayTv.Id))
                    {
                        var currentPayTvProductCompositeId = _offerTypeConstService.CurrentCompositePriceList.First(x => x.ProductSpecification.Id == ProductSpecificationEnum.PayTv.Id).ProductCompositeId;

                        //eslenigi varsa onu al
                        currentPayTvProductCompositeId = productCompositeEquivalentDic[currentPayTvProductCompositeId]?.SingleOrDefault(x => versions.Contains(x.Version))?.NewProductCompositeId ?? currentPayTvProductCompositeId;

                        searchResult = searchResult.Where(x => x.ProductComposites.Any(aa => aa.ProductCompositeId == currentPayTvProductCompositeId))?.ToList();
                    }



                    //todo
                    //Composite black list when change offer
                    //if (_offerTypeConstService.OfferTypes.Select(xa => xa.Id).Contains(OfferType.Upgrade.Id)
                    //    || _offerTypeConstService.OfferTypes.Select(xa => xa.Id).Contains(OfferType.CrossFromTvToInternet.Id)
                    //    || _offerTypeConstService.OfferTypes.Select(xa => xa.Id).Contains(OfferType.CrossFromInternetToTv.Id)
                    //     || _offerTypeConstService.OfferTypes.Select(xa => xa.Id).Contains(OfferType.CrossFromInternetToInternetGo.Id)
                    //    || _offerTypeConstService.OfferTypes.Select(xa => xa.Id).Contains(OfferType.ReverseCrossFromTvToInternet.Id)
                    //    || _offerTypeConstService.OfferTypes.Select(xa => xa.Id).Contains(OfferType.ReverseCrossFromInternetToTv.Id))
                    //{
                    //    //eslenigi varsa onu al
                    //    var currentCompositeIds = _offerTypeConstService.CurrentCompositePriceList?.Select(x => productCompositeEquivalentDic[x.ProductCompositeId]?.SingleOrDefault(x => versions.Contains(x.Version))?.NewProductCompositeId ?? x.ProductCompositeId)?.ToList();

                    //    if (currentCompositeIds != null && currentCompositeIds.Count > 0)
                    //    {
                    //        var blackList = productCompositeDic.Where(x => currentCompositeIds.Contains(x.ProductCompositeId) && x.BlackProductCompositeListWhenChangeOffer != null && x.BlackProductCompositeListWhenChangeOffer.Any())?.SelectMany(x => x.BlackProductCompositeListWhenChangeOffer)?.ToList();

                    //        if (blackList != null && blackList.Count > 0)
                    //        {
                    //            searchResult = searchResult.Where(x => !x.ProductComposites.Any(t => blackList.Contains(t.ProductCompositeId)))?.ToList();
                    //        }
                    //    }
                    //}

                    searchResult = searchResult.Where(x => _offerTypeConstService.OfferTypes.Select(xa => xa.Id).Contains(x.OfferType.Id)).ToList();


                    var priceClassDict = new Dictionary<string, IPriceCalculator>();

                    foreach (var className in searchResult.Where(x => x.PriceCalculationClass != null).Select(x => x.PriceCalculationClass.ClassName).Distinct())
                    {
                        var priceCalcClass = ActivatorUtilities.CreateInstance(_provider, Type.GetType(className)) as IPriceCalculator;

                        priceClassDict.Add(className, priceCalcClass);
                    }

                    var offerRuleList = searchResult.Where(x => x.ProductOfferRules != null && x.ProductOfferRules.Any()).GroupBy(x => x.ProductOffer.Id).Select(x => new { x.First().ProductOfferRules, ProductOfferId = x.Key });

                    Dictionary<Guid, bool> offerRuleResultDict = new Dictionary<Guid, bool>();



                    var offerDecoratorList = searchResult.Where(x => x.ProductOfferDecorators != null && x.ProductOfferDecorators.Any(y => y.Version == x.Version)).GroupBy(x => x.ProductOffer.Id).Select(x => new { x.First().ProductOfferDecorators.Single(y => y.Version == x.First().Version).ClassName, ProductOfferId = x.Key }).ToDictionary(x => x.ProductOfferId);

                    var offerDecoratorClassDict = new Dictionary<Guid, IOfferDecorator>();

                    foreach (var x in offerDecoratorList)
                    {
                        var decoratorClass = ActivatorUtilities.CreateInstance(_provider, Type.GetType(x.Value.ClassName)) as IOfferDecorator;

                        offerDecoratorClassDict.Add(x.Key, decoratorClass);
                    }

                    var offerHelper = new OfferHelper(_logger, _offerTypeConstService, _provider, _cacheProvider);



                    foreach (var entity in searchResult)
                    {
                        List<DynamicExpressoParameterModel> localDynamicExpressoParameters = new List<DynamicExpressoParameterModel>();
                        if (true)
                        {
                            try
                            {
                                IPriceCalculator priceCalcClass = null;
                                if (entity.PriceCalculationClass != null)
                                {
                                    priceCalcClass = priceClassDict[entity.PriceCalculationClass.ClassName];
                                }

                                var offer = new ProductOfferResponseModel
                                {
                                    BundleProduct = entity.Product,
                                    ProductOffer = entity.ProductOffer,
                                    ProductOfferCatalog = entity.ProductOfferCatalog,
                                    OfferRoutingType = entity.OfferRoutingType,
                                    Term = entity.Term,
                                    PaymentType = entity.PaymentType,
                                    OfferType = entity.OfferType,
                                    InstallmentsCount = entity.InstallmentsCount,
                                    StbOwnerOnly = entity.StbOwnerOnly,
                                    FeeType = entity.FeeType,
                                    Version = entity.Version
                                };
                                if (entity.CampaignDetailId != Guid.Empty)
                                {
                                    var campaignDetail = campaignDetailsDic[entity.CampaignDetailId];
                                    offer.CampaignDetail = new CampaignDetailResponseModel
                                    {
                                        CampaignDetailId = campaignDetail.CampaignDetailId,
                                        Name = campaignDetail.Name,
                                        Form = campaignDetail.Form
                                    };
                                }

                                offer.OTFs = entity.OTFs
                                        ?.Select(t => new OTFModel<decimal>
                                        {
                                            Default = t.Default,
                                            OTFId = t.OTFId,
                                            OTFType = t.OTFType,
                                            Price = new PriceModel<decimal>
                                            {
                                                Price = decimal.Parse(t.Price.Price),
                                                Duration = new Common.Models.DurationModel
                                                {
                                                    Duration = t.Price.Duration.Duration,
                                                    DurationType = t.Price.Duration.DurationType,
                                                },
                                                DurationStart = new Common.Models.DurationModel
                                                {
                                                    Duration = t.Price.DurationStart.Duration,
                                                    DurationType = t.Price.DurationStart.DurationType,
                                                }
                                            }
                                        }).ToList();

                                offer.ProductOfferRules = entity.ProductOfferRules
                                        ?.Select(t => new Common.Models.IdNameModel<Guid>
                                        {
                                            Id = t,
                                            Name = productOfferRuleDic[t].Formula
                                        }).ToList();

                                offer.ProductComposites = new List<ProductCompositeResponseModel>();


                                foreach (var t in entity.ProductComposites)
                                {
                                    try
                                    {
                                        var productCompositePrice = productCompositeList[t.ProductCompositeId].ProductCompositePrices.Single(tk => tk.Version == entity.Version);

                                        localDynamicExpressoParameters = decideClass.Calculate(localDynamicExpressoParameters, defaultRequestParameter.RuleParameters, request.OfferTypeId, entity.Version, productCompositePrice).GetAwaiter().GetResult();

                                        decimal price = 0;


                                        price = offerHelper.CalculatePrice(t.Price, productCompositePrice, localDynamicExpressoParameters, decideClass);

                                        var _newComposite = new ProductCompositeResponseModel
                                        {
                                            BillingProductCompositeId = productCompositeList[t.ProductCompositeId].BillingProductComposite.Id,
                                            ResourceCompositeBundleIds = productCompositeList[t.ProductCompositeId].ResourceCompositeBundles?.Select(asd => asd.Id).ToList(),

                                            ProductComposite = productCompositeList[t.ProductCompositeId].ProductComposite,
                                            ProductSpecification = productCompositeList[t.ProductCompositeId].ProductSpecification,
                                            //Price = offerHelper.CalculatePrice(t.Price, productCompositePrice, localDynamicExpressoParameters, decideClass),

                                            Price = price,
                                            ProductCompositePrice = new ProductCompositePriceResponseModel
                                            {
                                                ProductCompositePriceId = productCompositePrice.ProductCompositePriceId,
                                                ListPrice = productCompositePrice.ListPrice,
                                                MinPrice = productCompositePrice.MinPrice,
                                                WithoutContractPrice = productCompositePrice.WithoutContractPrice,
                                                Version = productCompositePrice.Version
                                            },
                                            Discounts = t.Discounts?.Select(k => new PriceModel<decimal>
                                            {
                                                Price = offerHelper.CalculatePrice(k.Price, productCompositePrice, localDynamicExpressoParameters, decideClass),
                                                Duration = new Common.Models.DurationModel
                                                {
                                                    Duration = k.Duration.Duration,
                                                    DurationType = k.Duration.DurationType,
                                                },
                                                DurationStart = new Common.Models.DurationModel
                                                {
                                                    Duration = k.DurationStart.Duration,
                                                    DurationType = k.DurationStart.DurationType,
                                                }
                                            }).ToList(),
                                            ExtraPrices = t.ExtraPrices?.ToDictionary(axc => axc.Key, axcc => axcc.Value),

                                            ProductComponents = productCompositeList[t.ProductCompositeId].ProductComponents
                                            ?.Select(k => new ProductComponentResponseModel
                                            {
                                                ProductComponent = new Common.Models.IdNameModel<Guid>
                                                {
                                                    Id = productComponentList.First(l => l.ProductComponentId == k).ProductComponentId,
                                                    Name = productComponentList.First(l => l.ProductComponentId == k).Name
                                                },

                                                ProductComponentPrices = productComponentList.First(l => l.ProductComponentId == k).ProductComponentPrices
                                                ?.Where(tk => tk.Version == entity.Version)
                                                .Select(l => new ProductComponentPriceResponseModel
                                                {
                                                    ProductComponentPriceId = l.ProductComponentPriceId,
                                                    Version = l.Version,
                                                    ListPrice = l.ListPrice,
                                                    MinPrice = l.MinPrice,
                                                    WithoutContractPrice = l.WithoutContractPrice
                                                }).ToList(),
                                                ProductComponentCharacteristicValues = productComponentList.First(l => l.ProductComponentId == k).ProductSpecificationCharacteristicValues
                                                ?.Select(l => new ProductComponentCharacteristicValueResponseModel
                                                {
                                                    ProductSpecificationCharacteristic = new Common.Models.IdNameModel<Guid>
                                                    {
                                                        Id = productSpecificationCharacteristicList.First(m => m.ProductSpecificationCharacteristicValues.Any(n => n.ProductSpecificationCharacteristicValueId == l)).ProductSpecificationCharacteristicId,
                                                        Name = productSpecificationCharacteristicList.First(m => m.ProductSpecificationCharacteristicValues.Any(n => n.ProductSpecificationCharacteristicValueId == l)).Name
                                                    },
                                                    ProductSpecificationCharacteristicDescription = productSpecificationCharacteristicList.First(m => m.ProductSpecificationCharacteristicValues.Any(n => n.ProductSpecificationCharacteristicValueId == l)).Description,
                                                    ProductSpecificationCharacteristicValueId = l,
                                                    Value = productSpecificationCharacteristicList.First(m => m.ProductSpecificationCharacteristicValues.Any(n => n.ProductSpecificationCharacteristicValueId == l)).ProductSpecificationCharacteristicValues.First(n => n.ProductSpecificationCharacteristicValueId == l).Value,
                                                    ValueType = productSpecificationCharacteristicList.First(m => m.ProductSpecificationCharacteristicValues.Any(n => n.ProductSpecificationCharacteristicValueId == l)).ProductSpecificationCharacteristicValues.First(n => n.ProductSpecificationCharacteristicValueId == l).ValueType,
                                                    Description = productSpecificationCharacteristicList.First(m => m.ProductSpecificationCharacteristicValues.Any(n => n.ProductSpecificationCharacteristicValueId == l)).ProductSpecificationCharacteristicValues.First(n => n.ProductSpecificationCharacteristicValueId == l).Description,
                                                    UnitOfMeasure = productSpecificationCharacteristicList.First(m => m.ProductSpecificationCharacteristicValues.Any(n => n.ProductSpecificationCharacteristicValueId == l)).ProductSpecificationCharacteristicValues.First(n => n.ProductSpecificationCharacteristicValueId == l).UnitOfMeasure,
                                                }).ToList(),



                                            }).ToList()
                                        };

                                        offer.ProductComposites.Add(_newComposite);

                                    }
                                    catch (Exception ex)
                                    {
                                        //TODO: burada oluşan hata loglanarak kontrol edilmeli.
                                    }
                                }

                                //entity.PriceCalculationClass = "=Round(ListPrice*0.8,0)";
                                if (entity.PriceCalculationClass != null)
                                {
                                    object[] prms = new[] { requestParemeters.SingleOrDefault(df => df.Property == "CompositeType")?.Value, requestParemeters.SingleOrDefault(df => df.Property == "TvAmount")?.Value };
                                    priceCalcClass.Calculate(localDynamicExpressoParameters, entity.PriceCalculationClass, offer, entity.Version, request.OfferTypeId, prms);

                                }

                                offer.ProductComposites = offer.ProductComposites.Select(x => { x.TotalPrice = x.Discounts == null || x.Discounts.Count() == 0 ? x.Price : x.Price - x.Discounts.Sum(y => y.Price); return x; }).ToList();
                                if (entity.BonusProductComposites != null && entity.BonusProductComposites.Any())
                                    offerHelper.FillBonusProductComposites(offer, entity.BonusProductComposites?.ToList(), entity.Version, productDic, productCompositeDic, productComponentDic, productSpecificationCharacteristicDic);

                                responseData.Add(offer);
                            }
                            catch (Exception ex)
                            {
                                _logger.LogError(ex, "Search Add {ProductOfferId}", entity.ProductOffer.Id);
                            }
                        }
                    }



                    if (request.CharacteristicList.Any(x => x.FirstOrDefault(k => k.Property == "Provider") != null && x.FirstOrDefault(k => k.Property == "Provider").Value == "TT"))
                    {
                        if (request.CharacteristicList.Any(x => x.FirstOrDefault(k => k.Property == "Provider") != null && x.FirstOrDefault(k => k.Property == "Provider").Value == "SOL"))
                        {

                        }
                        else
                        {
                            var withoutNakedSol = responseData.Where(x => !x.ProductComposites.Any(k => k.ProductComponents.Any(t => t.ProductComponentCharacteristicValues.Any(y => y.Value.ToLower() == "sol")))).ToList();
                            responseData = new ConcurrentBag<ProductOfferResponseModel>(withoutNakedSol);
                        }

                    }



                }


                //go+internet fix
                if (addGoproduct && _offerTypeConstService.CurrentGoComposite != null)
                {
                    foreach (var offer in responseData)
                    {
                        offer.ProductComposites.Add(new ProductCompositeResponseModel
                        {
                            BillingProductCompositeId = _offerTypeConstService.CurrentGoComposite.BillingProductCompositeId,
                            Price = _offerTypeConstService.CurrentGoComposite.Amount,
                            Discounts = new List<PriceModel<decimal>> { new PriceModel<decimal> { Duration = offer.Term, DurationStart = new Common.Models.DurationModel { Duration = 0, DurationType = DurationTypeEnum.Month }, IsPrimary = true, Price = _offerTypeConstService.CurrentGoComposite.Discount } },
                            ProductComposite = new Common.Models.IdNameModel<Guid> { Id = _offerTypeConstService.CurrentGoComposite.ProductComposite.Id, Name = _offerTypeConstService.CurrentGoComposite.ProductComposite.Name },
                            ProductCompositePrice = new ProductCompositePriceResponseModel(),
                            ProductSpecification = _offerTypeConstService.CurrentGoComposite.ProductSpecification,
                            ProductComponents = _offerTypeConstService.CurrentGoComposite.ProductComponents.Select(x => new ProductComponentResponseModel
                            {
                                ProductComponent = x.ProductComponent,
                                ProductComponentCharacteristicValues = x.ProductComponentCharacteristicValues.Select(y => new ProductComponentCharacteristicValueResponseModel
                                {
                                    Description = y.Description,
                                    ProductSpecificationCharacteristic = new Common.Models.IdNameModel<Guid> { Id = Guid.Parse(y.ProductSpecificationCharacteristic.Id), Name = y.ProductSpecificationCharacteristic.Name },
                                    ProductSpecificationCharacteristicDescription = y.Description,
                                    ProductSpecificationCharacteristicValueId = y.ProductSpecificationCharacteristicValueId,
                                    UnitOfMeasure = y.UnitOfMeasure,
                                    Value = y.Value,
                                    ValueType = y.ValueType
                                }).ToList(),
                                ProductComponentPrices = new List<ProductComponentPriceResponseModel>()



                            }).ToList()

                        });
                    }
                }
                var offerResponseData = FixBundleOffersPayTvDiscountsPrices(responseData);




                var response = new ApiSearchResponse<ProductOfferResponseModel>
                {
                    IsSuccess = true,
                };
                response.Data.Items = offerResponseData;
                response.Data.PageSize = responseData?.Count ?? 0;
                response.Data.TotalCount = responseData?.Count ?? 0;
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Search {@Request}", request);

                var response = new ApiSearchResponse<ProductOfferResponseModel>
                {
                    IsSuccess = false,
                    Message = ex.Message + " - " + ex.InnerException?.Message

                };
                return Ok(response);
            }
        }

        /// <summary>
        /// Sadece bundle tekliflerde, içinde internet ve payTv ürünlerini birlikte barındıran tekliflerde çalışır
        /// PayTv üzerinde hatalı indirimleri ve fatura sırasında iletim bedeli düşüldüğünde paytv ürünlerinin negatif sonuç vermesini engellmek için 
        /// ayrıca liste fiyatının üzerinde hesaplanan paytv fiyatları için indirim oranlarını düzenler
        /// fazla ya da hatalı görünen indirimi internet indirimine ekler ya da çıkarır.
        /// Toplam tutarı değiştirmez!!!
        /// </summary>
        /// <param name="responseData">Teklif Listesi</param>
        /// <returns></returns>
        private List<ProductOfferResponseModel> FixBundleOffersPayTvDiscountsPrices(ConcurrentBag<ProductOfferResponseModel> responseData)
        {
            ///Setting üzerine alınabilir.
            decimal itetimBedeli = (decimal)3.05;
            foreach (var item in responseData.Where(x => x.OfferRoutingType.Id == OfferRoutingTypeEnum.Tv_Go_Internet.Id || x.OfferRoutingType.Id == OfferRoutingTypeEnum.Tv_Internet.Id))
            {
                var internet = item.ProductComposites.FirstOrDefault(x => x.ProductSpecification.Id == ProductSpecificationEnum.Internet.Id);
                if (internet == null) continue;
                var payTv = item.ProductComposites.FirstOrDefault(x => x.ProductSpecification.Id == ProductSpecificationEnum.PayTv.Id);
                if (payTv == null || payTv.Discounts == null || payTv.Discounts.Count == 0 || payTv.Discounts.Sum(x => x.Price) == 0) continue;

                var totalDiscount = payTv.Discounts.Sum(x => x.Price);

                decimal indirimFazlasi = (totalDiscount + itetimBedeli) - payTv.Price;

                //Fazla indirim yapılmadıysa bu kaydı değiştirmez
                if (indirimFazlasi > 0)
                {

                    // indirim interneti de negatife çekiyor ise bu kaydı atla.
                    if (internet.Price - (internet.Discounts.Sum(x => x.Price) + indirimFazlasi) < 0) continue;

                    if (internet.Discounts == null || internet.Discounts.Count == 0) internet.Discounts = new List<PriceModel<decimal>>();

                    internet.Discounts.First().Price += indirimFazlasi;

                    int i = 0;
                    while (indirimFazlasi > 0 && i < payTv.Discounts.Count)
                    {
                        var paytvdiscount = payTv.Discounts[i];
                        if (paytvdiscount.Price >= indirimFazlasi)
                        {
                            paytvdiscount.Price -= indirimFazlasi;
                            indirimFazlasi = 0;
                        }
                        else
                        {
                            indirimFazlasi -= paytvdiscount.Price;
                            paytvdiscount.Price = 0;
                        }
                    }
                }
                var listPriceDiff = (payTv.Price - totalDiscount) - payTv.ProductCompositePrice.WithoutContractPrice;

                //Paytv fiyatı indirimler dahil liste fiyatından yüksek ise internet indiriminden fazla tutar kadar paytv indirimine aktarılır. 
                if (listPriceDiff > 0)
                {
                    // indirim interneti de negatife çekiyor ise bu kaydı atla.
                    if (internet.Discounts == null || internet.Discounts.Count == 0 || internet.Discounts.Sum(x => x.Price) - listPriceDiff < 0) continue;

                    internet.Discounts.First().Price -= listPriceDiff;

                    if (payTv.Discounts == null || payTv.Discounts.Count == 0) payTv.Discounts = new List<PriceModel<decimal>>();

                    payTv.Discounts.First().Price += listPriceDiff;
                }

            }




            return responseData.ToList();
        }

        [HttpPost(ProductOfferingApiMethodConst.ProductOffer.SearchForAddOns)]
        [ProducesResponseType(200, Type = typeof(ApiSearchResponse<ProductOfferResponseModel>))]
        public async Task<IActionResult> SearchProductOffers([FromBody] ProductOfferForAddOnRequestModel request)
        {
            try
            {
                //_logger.LogInformation(JsonConvert.SerializeObject(request));
                request.OfferTypeId = OfferType.AddOn.Id;

                var versions = VersionHelper.Find(request.OfferTypeId, null);

                var provider = _cacheProvider.GetCachingProvider("DefaultInMemory");
                if (!await provider.ExistsAsync("ProductOfferCache"))
                {
                    //set cache
                    await _cachingHelper.ResetCacheOfferSearch();
                }
                var productOfferDic = (await provider.GetAsync<Dictionary<Guid, ProductOffer>>("ProductOfferCache")).Value.Values;
                var productOfferCatalogDic = (await provider.GetAsync<Dictionary<Guid, ProductOfferCatalog>>("ProductOfferCatalogCache")).Value.Values;

                var productOfferRuleDic = (await provider.GetAsync<Dictionary<Guid, ProductOfferRule>>("ProductOfferRuleCache")).Value.Values.ToDictionary(x => x.ProductOfferRuleId);
                var requestParameterDic = (await provider.GetAsync<Dictionary<string, RequestParameter>>("RequestParameterCache")).Value.Values;

                var requestParemeters = await CharacteristicsParametersSplitterHelper.SplitParameters(request.CharacteristicList, _mongoRepository);

                var query = from offer in productOfferDic
                            join catalog in productOfferCatalogDic on offer.ProductOfferCatalogId equals catalog.ProductOfferCatalogId
                            where catalog.SalesChannels.Any(x => x == request.SalesChannel)
                              && catalog.OfferForms.Contains(request.OfferFormId)
                              && UserGroupIds.Any(x => catalog.Groups.Contains(x))
                              && offer.OfferType.Id == request.OfferTypeId
                              && catalog.StartDate <= DateTime.Now
                              && catalog.EndDate >= DateTime.Now
                              && offer.StartDate <= DateTime.Now
                              && offer.EndDate >= DateTime.Now
                            select new
                            {
                                ProductOffer = new Common.Models.IdNameModel<Guid> { Id = offer.ProductOfferId, Name = offer.Name },
                                ProductOfferCatalog = new Common.Models.IdNameModel<Guid> { Id = catalog.ProductOfferCatalogId, Name = catalog.Name },
                                OfferRoutingType = offer.OfferRoutingType,
                                Term = new Common.Models.DurationModel
                                {
                                    Duration = offer.Term.Duration,
                                    DurationType = offer.Term.DurationType,
                                },
                                ProductOfferRules = offer.ProductOfferRules,
                                PaymentType = offer.PaymentType,
                                InstallmentsCount = offer.InstallmentsCount,
                                StbOwnerOnly = offer.StbOwnerOnly,
                                FeeType = offer.FeeType,
                                offer.OfferType
                            };

                var searchResult = query.ToList();

                var responseData = new List<ProductOfferForAddOnResponseModel>();

                var dynamicExpressoParameters = new List<DynamicExpressoParameterModel>();
                if (requestParemeters != null && requestParemeters.Any())
                {
                    var properties = requestParemeters.Select(a => a.Property);
                    var requestParameters = requestParameterDic.Where(x => properties.Contains(x.Name)).ToList();
                    if (requestParameters != null && requestParameters.Any())
                    {
                        foreach (var requestParameter in requestParameters)
                        {
                            var decideClass = ActivatorUtilities.CreateInstance(_provider, Type.GetType(requestParameter.CalculationClass)) as IGetParameters;
                            foreach (var version in versions)
                            {
                                dynamicExpressoParameters = (await decideClass.Calculate(dynamicExpressoParameters, requestParameter.RuleParameters, request.OfferTypeId, version, requestParemeters.First(df => df.Property == requestParameter.Name).Value));
                            }
                        }
                    }
                }
                Parallel.ForEach(searchResult, entity =>
                {
                    try
                    {
                        bool isRulesSucceed = true;

                        #region Check Offer Rules

                        if (entity.ProductOfferRules != null && entity.ProductOfferRules.Any())
                        {
                            foreach (var productOfferRuleId in entity.ProductOfferRules)
                            {
                                var productOfferRule = productOfferRuleDic[productOfferRuleId];

                                isRulesSucceed = isRulesSucceed && bool.Parse(DynamicExpressoHelper.EvaluateAsync(new DynamicExpressoModel { Script = productOfferRule.Formula, Parameters = dynamicExpressoParameters }).ToString());
                            }
                        }

                        #endregion

                        if (isRulesSucceed)
                        {
                            var offer = new ProductOfferForAddOnResponseModel
                            {
                                ProductOffer = entity.ProductOffer,
                                ProductOfferCatalog = entity.ProductOfferCatalog,
                                OfferRoutingType = entity.OfferRoutingType,
                                OfferType = entity.OfferType,
                                Term = entity.Term,
                                PaymentType = entity.PaymentType,
                                InstallmentsCount = entity.InstallmentsCount,
                                FeeType = entity.FeeType
                            };

                            offer.ProductOfferRules = entity.ProductOfferRules
                                    ?.Select(t => new Common.Models.IdNameModel<Guid>
                                    {
                                        Id = t,
                                        Name = productOfferRuleDic[t].Formula
                                    }).ToList();

                            responseData.Add(offer);
                        }

                    }
                    catch (Exception ex)
                    {
                    }
                });

                var response = new ApiSearchResponse<ProductOfferForAddOnResponseModel>
                {
                    IsSuccess = true,
                };
                response.Data.Items = responseData;
                response.Data.PageSize = responseData?.Count ?? 0;
                response.Data.TotalCount = responseData?.Count ?? 0;

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SearchForAddon {@Request}", request);

                var response = new ApiSearchResponse<ProductOfferForAddOnResponseModel>
                {
                    IsSuccess = false,
                    Message = ex.Message + " - " + ex.InnerException?.Message

                };
                return Ok(response);
            }
        }


        [HttpPost(ProductOfferingApiMethodConst.ProductOffer.AddOnProducts)]
        [ProducesResponseType(200, Type = typeof(ApiResponse<AddOnProductsResponseModel>))]
        public async Task<IActionResult> AddOnProducts([FromBody] AddOnProductRequestModel request)
        {
            try
            {
                //_logger.LogInformation(JsonConvert.SerializeObject(request));

                var provider = _cacheProvider.GetCachingProvider("DefaultInMemory");
                if (!await provider.ExistsAsync("AddOnProductCache"))
                {
                    //set cache
                    await _cachingHelper.ResetCacheOfferSearch();
                }
                var addOnProductDic = (await provider.GetAsync<Dictionary<Guid, AddOnProduct>>("AddOnProductCache")).Value.Values;
                var addOnProductRuleDic = (await provider.GetAsync<Dictionary<Guid, AddOnProductRule>>("AddOnProductRuleCache")).Value.Values;
                var productDic = (await provider.GetAsync<Dictionary<Guid, Product>>("ProductCache")).Value.Values;
                var productOfferDic = (await provider.GetAsync<Dictionary<Guid, ProductOffer>>("ProductOfferCache")).Value.Values;
                var productOfferCatalogDic = (await provider.GetAsync<Dictionary<Guid, ProductOfferCatalog>>("ProductOfferCatalogCache")).Value.Values;

                var productCompositeDic = (await provider.GetAsync<Dictionary<Guid, ProductComposite>>("ProductCompositeCache")).Value.Values;
                var productComponentDic = (await provider.GetAsync<Dictionary<Guid, ProductComponent>>("ProductComponentCache")).Value.Values;
                var productOfferRuleDic = (await provider.GetAsync<Dictionary<Guid, ProductOfferRule>>("ProductOfferRuleCache")).Value.Values;
                var productSpecificationCharacteristicDic = (await provider.GetAsync<Dictionary<Guid, ProductSpecificationCharacteristic>>("ProductSpecificationCharacteristicCache")).Value.Values;
                var requestParameterDic = (await provider.GetAsync<Dictionary<string, RequestParameter>>("RequestParameterCache")).Value.Values;
                var productCompositeEquivalentDic = (await provider.GetAsync<Lookup<Guid, ProductCompositeEquivalents>>("ProductCompositeEquivalentsCache")).Value;

                var smartCityDic = (await provider.GetAsync<Dictionary<Guid, SmartCity>>("SmartCityCache")).Value.Values;
                //var campaignDetailsDic = (await provider.GetAsync<Dictionary<Guid, CampaignDetails>>("CampaignDetailsCache")).Value.Values;

                Guid productId = default;

                var productOffer = productOfferDic.Single(x => x.ProductOfferId == request.ProductOfferId);

                var getEquivalentProduct = false;//eski urunleri yeniisyle mapliyoruz nbu onu yapmaya yariyor.

                if (productOffer.OfferType.Id == OfferType.Raise.Id || productOffer.OfferType.Id == OfferType.Renew.Id || productOffer.OfferType.Id == OfferType.Upgrade.Id || productOffer.OfferType.Id == OfferType.TransferInternet.Id || productOffer.OfferType.Id == OfferType.AddOn.Id)
                {
                    getEquivalentProduct = true;
                }

                //go+internet fix
                if (request.CurrentProductCompositeAndProductSpecificationList.Any(x => x.Name == ProductSpecificationEnum.Internet.Id.ToString()) && !request.CurrentProductCompositeAndProductSpecificationList.Any(x => x.Name == ProductSpecificationEnum.PayTv.Id.ToString())
                && request.CurrentProductCompositeAndProductSpecificationList.Any(x => x.Name == ProductSpecificationEnum.Go.Id.ToString()) && (productOffer.OfferType.Id == OfferType.Raise.Id || productOffer.OfferType.Id == OfferType.Renew.Id || productOffer.OfferType.Id == OfferType.Upgrade.Id))
                {
                    var removeItem = request.CurrentProductCompositeAndProductSpecificationList.First(x => x.Name == ProductSpecificationEnum.Go.Id.ToString());
                    request.CurrentProductCompositeAndProductSpecificationList.Remove(removeItem);
                }

                int currentProductCompositeIdCount = request.CurrentProductCompositeAndProductSpecificationList.Count;
                //yalin compositeleri farkli oldugu icin bu sekilde.
                var nakedComposite = request.CurrentProductCompositeAndProductSpecificationList.SingleOrDefault(x => x.Name == ProductSpecificationEnum.Naked.Name);

                if (nakedComposite != null)
                {
                    var nakedCopositeList = productCompositeDic.Where(x => x.ProductSpecification.Id == ProductSpecificationEnum.Naked.Id).ToList();

                    request.CurrentProductCompositeAndProductSpecificationList.Remove(nakedComposite);

                    if (getEquivalentProduct)
                    {
                        var product = productDic.Where(x => request.CurrentProductCompositeAndProductSpecificationList.Select(c => productCompositeEquivalentDic[c.Id]?.SingleOrDefault(x => x.Version == request.Version)?.NewProductCompositeId ?? c.Id).All(t => x.ProductComposites.Contains(t)) && x.ProductComposites.Count == currentProductCompositeIdCount && nakedCopositeList.Any(z => x.ProductComposites.Contains(z.ProductCompositeId)));
                        productId = product.First().ProductId;
                    }
                    else
                    {

                        var product = productDic.Where(x => request.CurrentProductCompositeAndProductSpecificationList.Select(c => c.Id).All(t => x.ProductComposites.Contains(t)) && x.ProductComposites.Count == currentProductCompositeIdCount && nakedCopositeList.Any(z => x.ProductComposites.Contains(z.ProductCompositeId)));
                        productId = (product != null && product.Count() > 0) ? product.First().ProductId : request.BundleProductId;

                    }
                }
                else
                {
                    if (getEquivalentProduct)
                    {
                        var product = productDic.Where(x => request.CurrentProductCompositeAndProductSpecificationList.Select(c => productCompositeEquivalentDic[c.Id]?.SingleOrDefault(x => x.Version == request.Version)?.NewProductCompositeId ?? c.Id).All(t => x.ProductComposites.Contains(t)) && x.ProductComposites.Count == currentProductCompositeIdCount);
                        productId = product.First().ProductId;
                    }
                    else
                    {
                        var product = productDic.Where(x => request.CurrentProductCompositeAndProductSpecificationList.Select(c => c.Id).All(t => x.ProductComposites.Contains(t)) && x.ProductComposites.Count == currentProductCompositeIdCount);
                        productId = product.First().ProductId;
                    }
                }


                var addOnProductIds = new List<Guid>();

                //bu doluysa once bunula bak.
                if (request.AddOnProductIds != null && request.AddOnProductIds.Any())
                {
                    addOnProductIds = request.AddOnProductIds;
                }
                else if (request.AddOnProductCompositeIds != null)
                {
                    var addonProductQuery = (from rule in addOnProductRuleDic
                                             join addon in addOnProductDic on rule.AllowedAddOnProductId equals addon.AddOnProductId
                                             where rule.ProductOfferId == request.ProductOfferId && rule.ProductId == productId && addon.ProductComposites.Count == 1
                                             select addon).ToList();

                    foreach (var addonCompositeId in request.AddOnProductCompositeIds)
                    {
                        if (addonCompositeId == Guid.Parse("1f9898fc-42a6-4d5c-a20e-5f407ebc57ce"))
                        {
                            addonProductQuery = (from rule in addOnProductRuleDic
                                                 join addon in addOnProductDic on rule.AllowedAddOnProductId equals addon.AddOnProductId
                                                 where addon.AddOnProductId == Guid.Parse("31e3c5ba-cdf7-4136-b1f0-1debef8a85a4") && addon.ProductComposites.Count == 1
                                                 select addon).ToList();
                        }
                        var _composite = productCompositeEquivalentDic[addonCompositeId]?.SingleOrDefault(x => x.Version == request.Version)?.NewProductCompositeId ?? addonCompositeId;

                        var addonProductList = (from addon in addonProductQuery
                                                where addon.ProductComposites.All(x => x.ProductCompositeId == _composite)
                                                select addon);
                        if (addonProductList != null)
                        {
                            try
                            {
                                var addonProduct = addonProductList.SingleOrDefault();
                                if (addonProduct == null)
                                {

                                    // bu kampanyaya ait belgeselsever+ eğlencesever gibi ücretli addonlar taşınacak olduğu için forced olarak ekleniyor
                                    // bütün teklfilere bu ürünler addon kuralı olarak eklenemeyceği için statik olarak koyuldu 
                                    addonProductList = (from addon in addOnProductDic
                                                        where addon.ProductOfferId == _appSettings.AddonSaleOfferId && addon.ProductComposites.All(x => x.ProductCompositeId == _composite)
                                                        select addon);
                                    addonProduct = addonProductList.SingleOrDefault();
                                }
                                if (addonProduct != null)
                                    addOnProductIds.Add(addonProduct.AddOnProductId);
                            }
                            catch (Exception)
                            {

                            }

                        }

                    }
                }

                var addOnProductRules = addOnProductRuleDic.Where(x => x.ProductId == productId && (x.ProductOfferId == null || x.ProductOfferId == request.ProductOfferId)).ToList();


                var mustContainsAddOnProductIds = new List<Guid>();
                mustContainsAddOnProductIds.AddRange(addOnProductRules.Where(x => x.AllowedAddOnProductId.HasValue)?.Select(x => x.AllowedAddOnProductId.Value).ToList());
                mustContainsAddOnProductIds.AddRange(addOnProductRules.Where(x => x.ForcedAddOnProductId.HasValue)?.Select(x => x.ForcedAddOnProductId.Value).ToList());
                mustContainsAddOnProductIds.AddRange(addOnProductIds);

                var mustNotContainsCompositeIds = productDic.Where(x => x.ProductId == productId)?.SelectMany(x => x.ProductComposites).ToList();

                var query = from addOn in addOnProductDic
                            join product in productDic on addOn.ProductId equals product.ProductId
                            join offer in productOfferDic on addOn.ProductOfferId equals offer.ProductOfferId
                            where !addOn.ProductComposites.Any(t => mustNotContainsCompositeIds.Contains(t.ProductCompositeId))
                            && (addOn.ProductOfferId == request.ProductOfferId || addOn.ProductOfferId == _appSettings.AddonSaleOfferId)
                            && mustContainsAddOnProductIds.Contains(addOn.AddOnProductId)
                            && addOn.StartDate <= DateTime.Now
                            && addOn.EndDate >= DateTime.Now
                            && product.StartDate <= DateTime.Now
                            && product.EndDate >= DateTime.Now
                            && offer.StartDate <= DateTime.Now
                            && offer.EndDate >= DateTime.Now
                            select new
                            {
                                AddOnProduct = new Common.Models.IdNameModel<Guid> { Id = addOn.AddOnProductId, Name = addOn.Name },
                                Product = new Common.Models.IdNameModel<Guid> { Id = product.ProductId, Name = product.Name },
                                ProductOffer = new Common.Models.IdNameModel<Guid> { Id = offer.ProductOfferId, Name = offer.Name },
                                ProductComposites = addOn.ProductComposites,
                            };

                var searchResult = query.ToList();

                var responseData = new AddOnProductsResponseModel();
                if (searchResult != null && searchResult.Any())
                {
                    var productCompositeIds = searchResult.SelectMany(x => x.ProductComposites).Select(x => x.ProductCompositeId).Distinct().ToList();

                    var productCompositeList = (from composite in productCompositeDic
                                                where productCompositeIds.Contains(composite.ProductCompositeId)
                                                select new
                                                {
                                                    ProductComposite = new Common.Models.IdNameModel<Guid> { Id = composite.ProductCompositeId, Name = composite.Name },
                                                    composite.ProductSpecification,
                                                    composite.ProductComponents,
                                                    composite.BillingProductComposite,
                                                    composite.ResourceCompositeBundles,
                                                    composite.ProductCompositePrices,
                                                    composite.Neighborhoods
                                                }).ToList();


                    var productComponentIds = productCompositeList.SelectMany(x => x.ProductComponents).Distinct().ToList();
                    var productComponentList = productComponentDic.Where(x => productComponentIds.Contains(x.ProductComponentId)).ToList();
                    var productSpecificationCharacteristicValueIds = productComponentList.SelectMany(x => x.ProductSpecificationCharacteristicValues).Distinct().ToList();
                    var productSpecificationCharacteristicList = productSpecificationCharacteristicDic.Where(x => x.ProductSpecificationCharacteristicValues.Any(t => productSpecificationCharacteristicValueIds.Contains(t.ProductSpecificationCharacteristicValueId))).ToList();

                    var defaultRequestParameter = requestParameterDic.First(x => x.Name == "Default");
                    var decideClass = ActivatorUtilities.CreateInstance(_provider, Type.GetType(defaultRequestParameter.CalculationClass)) as IGetParameters;

                    var offerHelper = new OfferHelper(_logger, _offerTypeConstService, _provider, _cacheProvider);

                    foreach (var entity in searchResult)
                    {
                        try
                        {
                            bool isRulesSucceed = true;

                            //check  Neighborhood
                            if (request.NeighborhoodCode.HasValue && request.NeighborhoodCode > 0)
                            {
                                foreach (var item in entity.ProductComposites)
                                {
                                    if (productCompositeList.First(aa => aa.ProductComposite.Id == item.ProductCompositeId).Neighborhoods != null && productCompositeList.First(aa => aa.ProductComposite.Id == item.ProductCompositeId).Neighborhoods.Count > 0)
                                    {
                                        if (!productCompositeList.First(aa => aa.ProductComposite.Id == item.ProductCompositeId).Neighborhoods.Select(xa => xa.Neighborhood.Id).Any(xa => xa == request.NeighborhoodCode.Value))
                                        {
                                            isRulesSucceed = false;
                                        }
                                    }
                                }
                            }

                            var isSmartCity = false;

                            if (request.BuildingCode.HasValue && request.BuildingCode > 0)
                            {
                                isSmartCity = smartCityDic.Any(x => x.Building.Id == request.BuildingCode.Value);
                            }

                            //check  Building
                            foreach (var item in entity.ProductComposites)
                            {
                                if (productCompositeList.First(aa => aa.ProductComposite.Id == item.ProductCompositeId).ProductSpecification.Id == ProductSpecificationEnum.STB.Id)
                                {
                                    var componentIds = productCompositeList.First(aa => aa.ProductComposite.Id == item.ProductCompositeId).ProductComponents;
                                    var chracteristicValuesIds = productComponentList.Where(aa => componentIds.Contains(aa.ProductComponentId))?.SelectMany(x => x.ProductSpecificationCharacteristicValues).ToList();
                                    var chracteristicValues = productSpecificationCharacteristicDic.SelectMany(aa => aa.ProductSpecificationCharacteristicValues.Where(x => chracteristicValuesIds.Contains(x.ProductSpecificationCharacteristicValueId))).ToList();


                                    if (isSmartCity && chracteristicValues.All(x => !x.Value.Contains("QAM")))
                                    {
                                        isRulesSucceed = false;
                                    }


                                    //if (isSmartCity && chracteristicValues.All(x => !x.Value.Contains("QAM")))
                                    //{
                                    //    isRulesSucceed = false;
                                    //}
                                    //else if (!isSmartCity && chracteristicValues.Any(x => x.Value.Contains("QAM")))
                                    //{
                                    //    isRulesSucceed = false;
                                    //}



                                }
                            }

                            if (isRulesSucceed)
                            {
                                var addOn = new AddOnProductResponseModel
                                {
                                    AddOnProduct = entity.AddOnProduct,
                                    BundleProduct = entity.Product,
                                    ProductOffer = entity.ProductOffer,
                                    ProductComposites = new List<ProductCompositeResponseModel>()
                                };

                                foreach (var t in entity.ProductComposites)
                                {
                                    var _compositePrice = productCompositeList.Single(k => k.ProductComposite.Id == t.ProductCompositeId).ProductCompositePrices.Single(tk => tk.Version == request.Version);
                                    //statik ip için  fiyat
                                    var price = t.ProductCompositeId == Guid.Parse("dcad3022-963d-4955-a007-60c5fca4c2a4") ? (decimal)44.9 : offerHelper.CalculatePrice(t.Price, _compositePrice, defaultRequestParameter, productOffer.OfferType.Id, request.Version, decideClass);
                                    var _newComposite = new ProductCompositeResponseModel
                                    {
                                        BillingProductCompositeId = productCompositeList.First(k => k.ProductComposite.Id == t.ProductCompositeId).BillingProductComposite.Id,
                                        ResourceCompositeBundleIds = productCompositeList.First(k => k.ProductComposite.Id == t.ProductCompositeId).ResourceCompositeBundles?.Select(asd => asd.Id).ToList(),

                                        ProductComposite = productCompositeList.First(k => k.ProductComposite.Id == t.ProductCompositeId).ProductComposite,
                                        ProductSpecification = productCompositeList.First(k => k.ProductComposite.Id == t.ProductCompositeId).ProductSpecification,
                                        Price = price,
                                        ProductCompositePrice = new ProductCompositePriceResponseModel
                                        {
                                            ProductCompositePriceId = _compositePrice.ProductCompositePriceId,
                                            ListPrice = _compositePrice.ListPrice,
                                            MinPrice = _compositePrice.MinPrice,
                                            WithoutContractPrice = _compositePrice.WithoutContractPrice,
                                            Version = _compositePrice.Version
                                        },
                                        Discounts = t.Discounts?.Select(k => new PriceModel<decimal>
                                        {
                                            Price = decimal.Parse(offerHelper.CalculatePrice(k.Price, _compositePrice, defaultRequestParameter, productOffer.OfferType.Id, request.Version, decideClass).ToString()),
                                            Duration = new Common.Models.DurationModel
                                            {
                                                Duration = k.Duration.Duration,
                                                DurationType = k.Duration.DurationType,
                                            },
                                            DurationStart = new Common.Models.DurationModel
                                            {
                                                Duration = k.DurationStart.Duration,
                                                DurationType = k.DurationStart.DurationType,
                                            }
                                        }).ToList(),
                                        ExtraPrices = t.ExtraPrices?.ToDictionary(axc => axc.Key, axcc => axcc.Value),

                                        ProductComponents = productCompositeList.First(k => k.ProductComposite.Id == t.ProductCompositeId).ProductComponents
                                         ?.Select(k => new ProductComponentResponseModel
                                         {
                                             ProductComponent = new Common.Models.IdNameModel<Guid>
                                             {
                                                 Id = productComponentList.First(l => l.ProductComponentId == k).ProductComponentId,
                                                 Name = productComponentList.First(l => l.ProductComponentId == k).Name
                                             },

                                             ProductComponentPrices = productComponentList.First(l => l.ProductComponentId == k).ProductComponentPrices
                                             ?.Where(tk => tk.Version == request.Version)
                                             .Select(l => new ProductComponentPriceResponseModel
                                             {
                                                 ProductComponentPriceId = l.ProductComponentPriceId,
                                                 Version = l.Version,
                                                 ListPrice = l.ListPrice,
                                                 MinPrice = l.MinPrice,
                                                 WithoutContractPrice = l.WithoutContractPrice
                                             }).ToList(),

                                             ProductComponentCharacteristicValues = productComponentList.First(l => l.ProductComponentId == k).ProductSpecificationCharacteristicValues
                                             ?.Select(l => new ProductComponentCharacteristicValueResponseModel
                                             {
                                                 ProductSpecificationCharacteristic = new Common.Models.IdNameModel<Guid>
                                                 {
                                                     Id = productSpecificationCharacteristicList.First(m => m.ProductSpecificationCharacteristicValues.Any(n => n.ProductSpecificationCharacteristicValueId == l)).ProductSpecificationCharacteristicId,
                                                     Name = productSpecificationCharacteristicList.First(m => m.ProductSpecificationCharacteristicValues.Any(n => n.ProductSpecificationCharacteristicValueId == l)).Name
                                                 },
                                                 ProductSpecificationCharacteristicDescription = productSpecificationCharacteristicList.First(m => m.ProductSpecificationCharacteristicValues.Any(n => n.ProductSpecificationCharacteristicValueId == l)).Description,
                                                 ProductSpecificationCharacteristicValueId = l,
                                                 Value = productSpecificationCharacteristicList.First(m => m.ProductSpecificationCharacteristicValues.Any(n => n.ProductSpecificationCharacteristicValueId == l)).ProductSpecificationCharacteristicValues.First(n => n.ProductSpecificationCharacteristicValueId == l).Value,
                                                 ValueType = productSpecificationCharacteristicList.First(m => m.ProductSpecificationCharacteristicValues.Any(n => n.ProductSpecificationCharacteristicValueId == l)).ProductSpecificationCharacteristicValues.First(n => n.ProductSpecificationCharacteristicValueId == l).ValueType,
                                                 Description = productSpecificationCharacteristicList.First(m => m.ProductSpecificationCharacteristicValues.Any(n => n.ProductSpecificationCharacteristicValueId == l)).ProductSpecificationCharacteristicValues.First(n => n.ProductSpecificationCharacteristicValueId == l).Description,
                                                 UnitOfMeasure = productSpecificationCharacteristicList.First(m => m.ProductSpecificationCharacteristicValues.Any(n => n.ProductSpecificationCharacteristicValueId == l)).ProductSpecificationCharacteristicValues.First(n => n.ProductSpecificationCharacteristicValueId == l).UnitOfMeasure,
                                             }).ToList()

                                         }).ToList()
                                    };

                                    addOn.ProductComposites.Add(_newComposite);
                                }

                                if (addOn.ProductOffer.Id == _appSettings.AddonSaleOfferId && request.AddOnProductCompositeIds.Any(k => k == addOn.ProductComposites.FirstOrDefault().ProductComposite.Id))
                                {
                                    // bu kampanyaya ait belgeselsever+ eğlencesever gibi ücretli addonlar taşınacak olduğu için forced olarak ekleniyor
                                    // bütün teklfilere bu ürünler addon kuralı olarak eklenemeyceği için statik olarak koyuldu 
                                    responseData.Selected.Add(addOn);
                                    responseData.Forced.Add(addOn.AddOnProduct);
                                }
                                else if (addOnProductRules != null && addOnProductRules.Any(x => x.ForcedAddOnProductId == addOn.AddOnProduct.Id))
                                {
                                    responseData.Selected.Add(addOn);
                                    responseData.Forced.Add(addOn.AddOnProduct);
                                }

                                else if (addOnProductIds.Contains(addOn.AddOnProduct.Id))
                                {
                                    responseData.Selected ??= new List<AddOnProductResponseModel>();
                                    responseData.Selected.Add(addOn);
                                }
                                else
                                {
                                    responseData.Allowed ??= new List<AddOnProductResponseModel>();
                                    responseData.Allowed.Add(addOn);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "AddonProducts loop {ProductOfferId}", entity.ProductOffer.Id);
                        }
                    }

                    //Forbidden (selected olanlarda ki forbidden)
                    if (addOnProductIds.Any() && addOnProductRules != null && addOnProductRules.Any() && responseData.Selected != null && responseData.Selected.Any())
                    {
                        var forbiddenAddOnProductIds = addOnProductRules
                            .Where(x => (x.AllowedAddOnProductId.HasValue && x.ForbiddenAddOnProductIds != null && x.ForbiddenAddOnProductIds.Any()))
                            .Where(x => responseData.Selected.Any(t => t.AddOnProduct.Id == x.AllowedAddOnProductId))
                            ?.SelectMany(x => x.ForbiddenAddOnProductIds)
                            .Distinct()
                            .ToList();
                        //var staticIpId = addOnProductRules
                        //   .Where(x => (x.Description.Contains("(V) Statik IP")))
                        //   .Where(x => responseData.Selected.Any(t => t.AddOnProduct.Id == x.AllowedAddOnProductId))
                        //   ?.Select(x => x.AllowedAddOnProductId)
                        //   .Distinct()
                        //   .ToList();
                        //if (staticIpId != null && staticIpId.Count() > 0 && productOffer.OfferType.Id != OfferType.New.Id && productOffer.OfferType.Id != OfferType.AddOn.Id)
                        //{
                        //    forbiddenAddOnProductIds.Add(Guid.Parse(staticIpId.First().ToString()));
                        //}

                        if (forbiddenAddOnProductIds != null && forbiddenAddOnProductIds.Any())
                        {
                            foreach (var forbiddenAddOnProductId in forbiddenAddOnProductIds)
                            {
                                var forbiddenAddOnProduct = addOnProductDic.First(x => x.AddOnProductId == forbiddenAddOnProductId);

                                responseData.Forbidden ??= new List<Common.Models.IdNameModel<Guid>>();
                                if (responseData.Allowed.Any(x => x.AddOnProduct.Id == forbiddenAddOnProduct.AddOnProductId))
                                {
                                    try
                                    {
                                        responseData.Allowed.Remove(responseData.Allowed.First(x => x.AddOnProduct.Id == forbiddenAddOnProduct.AddOnProductId));
                                    }
                                    catch (Exception)
                                    {

                                    }

                                    responseData.Forbidden.Add(new Common.Models.IdNameModel<Guid>
                                    {
                                        Id = forbiddenAddOnProduct.AddOnProductId,
                                        Name = forbiddenAddOnProduct.Name
                                    });
                                }
                                if (responseData.Selected.Any(x => x.AddOnProduct.Id == forbiddenAddOnProduct.AddOnProductId))
                                {
                                    responseData.Selected.Remove(responseData.Selected.First(x => x.AddOnProduct.Id == forbiddenAddOnProduct.AddOnProductId));
                                }
                            }
                        }
                    }
                }

                var offerResponseData = FixModemAddOnPrices(responseData, request.CustomerAccountId.ToString());
                var response = new ApiResponse<AddOnProductsResponseModel>
                {
                    IsSuccess = true,
                    Data = responseData
                };
                return Ok(response);
            }
            catch (System.Exception ex)
            {
                _logger.LogError(ex, "AddonProducts {@Request}", request);

                var response = new ApiResponse<AddOnProductsResponseModel>
                {
                    IsSuccess = false,
                    Message = ex.Message + " - " + ex.InnerException?.Message

                };
                return Ok(response);
            }
        }

        /// <summary>
        /// Teklif içinde bununan modem add on ları için fiyatları aşağıdaki şekilde düzenler
        /// Kablosuz ADSL Modem ve Kablosuz VDSL2 Modem için  Modeme ödenen tutar;
        /// 0-2,99 TL --> yeni tutar 6 TL 
        ///  TL - 4,49 TL --> yeni tutar 7 TL
        /// “4,50 - 9,5 TL --> yeni tutar 9,5 TL 
        /// ** FTTH altyapılarında modem ücreti aşağıdaki gibi olacaktır.
        /// 0TL - 9,5 TL --> 9,5 TL
        /// </summary>
        /// <param name="responseData"></param>
        /// <returns></returns>
        private AddOnProductsResponseModel FixModemAddOnPrices(AddOnProductsResponseModel responseData, string customerAccountId)
        {
            var dynamicExpressoParameters = new List<DynamicExpressoParameterModel>();
            var decideClass = ActivatorUtilities.CreateInstance(_provider, Type.GetType("Microservice.ProductOffering.API.DecideClasses.GetCustomerAccountParameters, Microservice.ProductOffering.API")) as IGetPrices;

            dynamicExpressoParameters = decideClass.GetCurrentPrices(dynamicExpressoParameters, customerAccountId).GetAwaiter().GetResult();
            var modemPrice = dynamicExpressoParameters.FirstOrDefault(x => x.Name == "CurrentModemPrice");
            var solModem = new Guid("55eaf46c-2b0d-47ee-9c05-1d7ad8b55ed8");
            if (modemPrice != null)
            {
                foreach (var item in responseData.Allowed)
                {
                    foreach (var addOn in item.ProductComposites.Where(x => x.ProductSpecification.Id == ProductSpecificationEnum.Modem.Id && x.ProductComposite.Id != solModem))
                    {
                        addOn.Price = GetModemPrice(addOn, Convert.ToDecimal(modemPrice.Value));

                    }
                }
                foreach (var item in responseData.Selected)
                {
                    foreach (var addOn in item.ProductComposites.Where(x => x.ProductSpecification.Id == ProductSpecificationEnum.Modem.Id && x.ProductComposite.Id != solModem))
                    {
                        addOn.Price = GetModemPrice(addOn, Convert.ToDecimal(modemPrice.Value));
                    }
                }
            }

            return responseData;
        }

        private decimal GetModemPrice(ProductCompositeResponseModel addOn, decimal currentAmount)
        {


            var addOnOfferPrice = addOn.Price - addOn.Discounts.Sum(x => x.Price);
            decimal newAmount = 0;

            if (addOn.ProductComposite.Name.ToLower().Contains("ftth") || addOn.ProductComposite.Name.ToLower().Contains("fiber"))
            {
                if (currentAmount <= 7)
                {
                    newAmount = (decimal)10;
                }
                else if (currentAmount > 7 && currentAmount <= 15)
                {
                    newAmount = (decimal)(15);
                }
                else
                {
                    newAmount = addOn.Price;
                }

            }
            else if (currentAmount < (decimal)5.5)
            {
                newAmount = (decimal)8.5;
            }
            else if (currentAmount < (decimal)9)
            {
                newAmount = (decimal)10.5;
            }
            else if (currentAmount <= (decimal)12)
            {
                newAmount = (decimal)12;
            }
            else
            {
                newAmount = addOn.Price;
            }


            if (addOnOfferPrice > newAmount)
            {
                return addOn.Price - (addOnOfferPrice - newAmount);
            }
            else if (newAmount == addOnOfferPrice)
            {
                return addOn.Price;
            }
            else
            {
                if (addOnOfferPrice > newAmount)
                {
                    return addOn.Price + (addOnOfferPrice - newAmount);
                }
                else
                {
                    return addOn.Price + (newAmount - addOnOfferPrice);

                }
            }


            //if (addOn.ProductComposite.Name.ToLower().Contains("ftth"))
            //{
            //    return 9.5M;
            //}
            //else if (addOnOfferPrice < 3)
            //{
            //    return 6M;
            //}
            //else if (addOnOfferPrice < 4.5M)
            //{
            //    return 7M;
            //}
            //else
            //{
            //    return 9.5M;
            //}
        }

        [HttpPost(ProductOfferingApiMethodConst.ProductOffer.ValidateForActivation)]
        [ProducesResponseType(200, Type = typeof(ApiResponse))]
        public async Task<IActionResult> ValidateForActivation([FromBody] ValidateCampaignRequestModel request)
        {
            //_logger.LogInformation(JsonConvert.SerializeObject(request));

            var offer = await _mongoRepository.GetAsync<ProductOffer>(x => x.ProductOfferId == request.ProductOfferId);
            if (offer != null && request.CampaignDetailId != Guid.Empty && offer.CampaignDetailId == request.CampaignDetailId)
            {
                try
                {
                    request.PropertyValues.Add(new PropertyValueModel { Property = "ProductId", Value = request.ProductId.ToString() });
                    request.PropertyValues.Add(new PropertyValueModel { Property = "ProductOfferId", Value = request.ProductOfferId.ToString() });
                    request.PropertyValues.Add(new PropertyValueModel { Property = "ProductOfferCatalogId", Value = offer.ProductOfferCatalogId.ToString() });

                    var campaignDetail = await _mongoRepository.GetAsync<CampaignDetails>(x => x.CampaignDetailId == request.CampaignDetailId);

                    var decideClass = ActivatorUtilities.CreateInstance(_provider, Type.GetType(campaignDetail.Class)) as Application.Campaigns.Campaign;
                    var result = await decideClass.ValidateForActivation(request.PropertyValues);

                    return Ok(new ApiResponse(result));
                }
                catch (Exception ex)
                {
                    return Ok(new ApiResponse(ex.Message));
                }
            }
            else
            {
                return Ok(new ApiResponse("Bu teklifin formu bulunmuyor!"));
            }
        }


        [HttpPost(ProductOfferingApiMethodConst.ProductOffer.OTFs)]
        [ProducesResponseType(200, Type = typeof(ApiResponse<List<OTFModel<decimal>>>))]
        public async Task<IActionResult> GetProductOfferOTFs([FromBody] GetProductOfferOTFsRequestModel request)
        {
            //_logger.LogInformation(JsonConvert.SerializeObject(request));

            var responseData = new List<OTFModel<decimal>>();

            if (request.OfferTypeId == OfferType.AddOn.Id ||
                request.OfferTypeId == OfferType.Renew.Id ||
                request.OfferTypeId == OfferType.Raise.Id ||
                request.OfferTypeId == OfferType.Upgrade.Id ||
                request.OfferTypeId == OfferType.ReverseCrossFromInternetGoToInternet.Id ||
                request.OfferTypeId == OfferType.ReverseCrossFromTvToInternet.Id || request.OfferTypeId == OfferType.SolSwap.Id ||
                request.OfferTypeId == OfferType.ReverseCrossFromInternetToTv.Id)
            {
                //if(request.OfferTypeId== OfferType.Renew.Id && request.AddonCompositeList!= null && request.AddonCompositeList.Any(k=>k==Guid.Parse("00e0f5b0-178d-4fc3-a156-fc2349a13bd3")))
                //{
                //    responseData.Add(new OTFModel<decimal>
                //    {
                //        Default = true,
                //        OTFId = Guid.Parse("90941328-40fa-4b46-8449-5e5ba867a994"),
                //        OTFType = OTFTypeEnum.AntivirusActivation,
                //        Price = new PriceModel<decimal>
                //        {
                //            Price = decimal.Parse("939"),
                //            Duration = new Common.Models.DurationModel
                //            {
                //                Duration = 0,
                //                DurationType = DurationTypeEnum.Month
                //            },
                //            DurationStart = new Common.Models.DurationModel
                //            {
                //                Duration = 0,
                //                DurationType = DurationTypeEnum.Month
                //            }
                //        }
                //    });
                //    responseData.Add(new OTFModel<decimal>
                //    {
                //        Default = true,
                //        OTFId = Guid.Parse("95a6ec01-ac95-4d08-a2dd-faee87b0b621"),
                //        OTFType = OTFTypeEnum.AntivirusDiscount,
                //        Price = new PriceModel<decimal>
                //        {
                //            Price = decimal.Parse("-939"),
                //            Duration = new Common.Models.DurationModel
                //            {
                //                Duration = 0,
                //                DurationType = DurationTypeEnum.Month
                //            },
                //            DurationStart = new Common.Models.DurationModel
                //            {
                //                Duration = 0,
                //                DurationType = DurationTypeEnum.Month
                //            }
                //        }
                //    });
                //}

                return Ok(new ApiResponse<List<OTFModel<decimal>>>(responseData));
            }
            if (request.OfferTypeId == OfferType.TransferInternet.Id)
            {
                if (request.InternetProviderCharacteristicValue == "SOL")
                {
                    responseData.Add(new OTFModel<decimal>
                    {
                        Default = true,
                        OTFId = Guid.Parse("E1303B75-0926-4FC8-A10C-6C1D1FDCB446"),
                        OTFType = OTFTypeEnum.Transference,
                        Price = new PriceModel<decimal>
                        {
                            Price = decimal.Parse("480"),
                            Duration = new Common.Models.DurationModel
                            {
                                Duration = 0,
                                DurationType = DurationTypeEnum.Month
                            },
                            DurationStart = new Common.Models.DurationModel
                            {
                                Duration = 0,
                                DurationType = DurationTypeEnum.Month
                            }
                        },
                        InstallmentCount=0
                    });
                }
                else if (request.InternetProviderCharacteristicValue == "TT")
                {
                    responseData.Add(new OTFModel<decimal>
                    {
                        Default = true,
                        OTFId = Guid.Parse("E64DD375-A8ED-42FB-A108-2194174274C3"),
                        OTFType = OTFTypeEnum.Transference,
                        Price = new PriceModel<decimal>
                        {
                            Price = decimal.Parse("89"),
                            Duration = new Common.Models.DurationModel
                            {
                                Duration = 0,
                                DurationType = DurationTypeEnum.Month
                            },
                            DurationStart = new Common.Models.DurationModel
                            {
                                Duration = 0,
                                DurationType = DurationTypeEnum.Month
                            }
                        },
                        InstallmentCount = 0
                    });
                }

                return Ok(new ApiResponse<List<OTFModel<decimal>>>(responseData));
            }

            var offer = await _mongoRepository.GetAsync<ProductOffer>(x => x.ProductOfferId == request.ProductOfferId);
            var product = await _mongoRepository.GetAsync<Product>(x => x.ProductId == request.ProductId);
            if (offer != null && product != null)
            {
                var productComposites = await _mongoRepository.SearchAsync<ProductComposite>(x => product.ProductComposites.Contains(x.ProductCompositeId), 0, 1000);

                try
                {
                    foreach (var productComposite in productComposites)
                    {



                        if (productComposite.ProductSpecification.Id == ProductSpecificationEnum.PayTv.Id && (request.OfferTypeId == OfferType.New.Id || request.OfferTypeId == OfferType.CrossFromInternetToTv.Id))
                        {
                            //TV OTFs

                            //aktivasyon
                            responseData.Add(new OTFModel<decimal>
                            {
                                Default = true,
                                OTFId = Guid.Parse("f4f47e23-775e-42f5-811b-7834be180b17"),
                                OTFType = OTFTypeEnum.TvActivation,
                                Price = new PriceModel<decimal>
                                {
                                    Price = decimal.Parse("60"),
                                    Duration = new Common.Models.DurationModel
                                    {
                                        Duration = offer.Term.Duration,
                                        DurationType = offer.Term.DurationType
                                    },
                                    DurationStart = new Common.Models.DurationModel
                                    {
                                        Duration = 0,
                                        DurationType = DurationTypeEnum.Second
                                    }
                                },
                                InstallmentCount = 0
                            });

                            //aktivasyon indirimi
                            responseData.Add(new OTFModel<decimal>
                            {
                                Default = true,
                                OTFId = Guid.Parse("53eadb6a-f6fa-4e49-a6fc-4d6ddc4b1e8b"),
                                OTFType = OTFTypeEnum.TvActivationDiscount,
                                Price = new PriceModel<decimal>
                                {
                                    Price = decimal.Parse("-60"),
                                    Duration = new Common.Models.DurationModel
                                    {
                                        Duration = offer.Term.Duration,
                                        DurationType = offer.Term.DurationType
                                    },
                                    DurationStart = new Common.Models.DurationModel
                                    {
                                        Duration = 0,
                                        DurationType = DurationTypeEnum.Second
                                    }
                                },
                                InstallmentCount = 0
                            });

                            //kurulum


                            responseData.Add(new OTFModel<decimal>
                            {
                                Default = true,
                                OTFId = Guid.Parse("992b4089-a7a6-49de-84a9-7be33d3caacf"),
                                OTFType = OTFTypeEnum.TvInstallation,
                                Price = new PriceModel<decimal>
                                {
                                    Price = decimal.Parse("120"),
                                    Duration = new Common.Models.DurationModel
                                    {
                                        Duration = offer.Term.Duration,
                                        DurationType = offer.Term.DurationType
                                    },
                                    DurationStart = new Common.Models.DurationModel
                                    {
                                        Duration = 0,
                                        DurationType = DurationTypeEnum.Second
                                    }
                                },
                                InstallmentCount = 0
                            });
                            //kurulum indirimi
                            responseData.Add(new OTFModel<decimal>
                            {
                                Default = true,
                                OTFId = Guid.Parse("57bd59c8-166b-418f-b6a1-5a61bf492bef"),
                                OTFType = OTFTypeEnum.TvInstallationDiscount,
                                Price = new PriceModel<decimal>
                                {
                                    Price = decimal.Parse("-120"),
                                    Duration = new Common.Models.DurationModel
                                    {
                                        Duration = offer.Term.Duration,
                                        DurationType = offer.Term.DurationType
                                    },
                                    DurationStart = new Common.Models.DurationModel
                                    {
                                        Duration = 0,
                                        DurationType = DurationTypeEnum.Second
                                    }
                                },
                                InstallmentCount = 0
                            });
                        }
                        else if (productComposite.ProductSpecification.Id == ProductSpecificationEnum.Internet.Id && (request.OfferTypeId == OfferType.New.Id || request.OfferTypeId == OfferType.ReverseCrossFromTvToInternet.Id))
                        {
                            //aktivasyon bütün internet ürünleri için geçerli

                            //aktivasyon
                            responseData.Add(new OTFModel<decimal>
                            {
                                Default = true,
                                OTFId = Guid.Parse("da812d7d-cd2b-4a04-b8e6-81668b783000"),
                                OTFType = OTFTypeEnum.InternetActivation,
                                Price = new PriceModel<decimal>
                                {
                                    Price = decimal.Parse("264"),
                                    Duration = new Common.Models.DurationModel
                                    {
                                        Duration = offer.Term.Duration,
                                        DurationType = offer.Term.DurationType
                                    },
                                    DurationStart = new Common.Models.DurationModel
                                    {
                                        Duration = 0,
                                        DurationType = DurationTypeEnum.Second
                                    }
                                },
                                InstallmentCount = 0
                            });
                            //aktivasyon indirimi
                            responseData.Add(new OTFModel<decimal>
                            {
                                Default = true,
                                OTFId = Guid.Parse("8eb36f2a-60ef-4aad-ab13-ec88f93b250e"),
                                OTFType = OTFTypeEnum.InternetActivationDiscount,
                                Price = new PriceModel<decimal>
                                {
                                    Price = decimal.Parse("-264"),
                                    Duration = new Common.Models.DurationModel
                                    {
                                        Duration = offer.Term.Duration,
                                        DurationType = offer.Term.DurationType
                                    },
                                    DurationStart = new Common.Models.DurationModel
                                    {
                                        Duration = 0,
                                        DurationType = DurationTypeEnum.Second
                                    }
                                },
                                InstallmentCount = 0
                            });

                            if (request.InternetProviderCharacteristicValue == "SOL")
                            {
                                //SOL OTFs

                                //kurulum
                                DateTime dt2023Year = new DateTime(2023, 1, 1);
                                string solPrice = "400";
                                if (DateTime.Now >= dt2023Year)
                                {
                                    solPrice = "1100";
                                }


                                responseData.Add(new OTFModel<decimal>
                                {
                                    Default = true,
                                    OTFId = Guid.Parse("0061deff-de83-4c53-b1c8-ae96674bac0a"),
                                    OTFType = OTFTypeEnum.SOLActivation,
                                    Price = new PriceModel<decimal>
                                    {
                                        Price = decimal.Parse("500"),
                                        Duration = new Common.Models.DurationModel
                                        {
                                            Duration = offer.Term.Duration,
                                            DurationType = offer.Term.DurationType
                                        },
                                        DurationStart = new Common.Models.DurationModel
                                        {
                                            Duration = 0,
                                            DurationType = DurationTypeEnum.Second
                                        }
                                    },
                                    InstallmentCount = 0
                                });
                                //kurulum indirimi
                                responseData.Add(new OTFModel<decimal>
                                {
                                    Default = true,
                                    OTFId = Guid.Parse("305e006b-5ef7-4a21-a088-dd09ca82d1a3"),
                                    OTFType = OTFTypeEnum.SOLActivationDiscount,
                                    Price = new PriceModel<decimal>
                                    {
                                        Price = decimal.Parse("-500"),
                                        Duration = new Common.Models.DurationModel
                                        {
                                            Duration = offer.Term.Duration,
                                            DurationType = offer.Term.DurationType
                                        },
                                        DurationStart = new Common.Models.DurationModel
                                        {
                                            Duration = 0,
                                            DurationType = DurationTypeEnum.Second
                                        }
                                    },
                                    InstallmentCount = 0
                                });

                            }
                            else
                            {
                                //TT OTFs

                                //Kurulum ücretleri ISS geçiş, PSTN ve TT FTTH fiber satışlarında bulunmuyor.
                                if (request.InternetInfrastructureCharacteristicValue != "FTTH"
                                    || request.InternetSaleTypeId == InternetSaleTypeEnum.Ndsl.Id
                                    || request.InternetSaleTypeId == InternetSaleTypeEnum.Pstn_To_Ndsl.Id)
                                {
                                    //kurulum

                                    int duration = 0;
                                    decimal price = 150;
                                    bool isCommintment = true;
                                    DateTime year2023 = new DateTime(2023, 1, 1);
                                    if (DateTime.Now >= year2023)
                                    {
                                        price = 250;
                                    }
                                    if (offer != null && offer.Name.Trim().Contains("Taahhütsüz") && offer.Name.Trim().Contains("TK"))
                                    {
                                        duration = Convert.ToInt32(offer.Name.Trim().Substring((Regex.Match(offer.Name.Trim(), "TK").Index - 1), 1));
                                        price = 270;
                                        isCommintment = false;
                                    }
                                    else
                                    {
                                        duration = offer.Term.Duration;
                                    }

                                    responseData.Add(new OTFModel<decimal>
                                    {
                                        Default = true,
                                        OTFId = Guid.Parse("41d376c3-4f37-4eb2-bb44-9716407ca2dd"),
                                        OTFType = OTFTypeEnum.TTActivation,
                                        Price = new PriceModel<decimal>
                                        {
                                            Price = price,
                                            Duration = new Common.Models.DurationModel
                                            {
                                                Duration = offer.Term.Duration,
                                                DurationType = offer.Term.DurationType
                                            },
                                            DurationStart = new Common.Models.DurationModel
                                            {
                                                Duration = 0,
                                                DurationType = DurationTypeEnum.Second
                                            }
                                        },
                                        InstallmentCount = isCommintment ? 0:duration
                                    });
                                    //kurulum indirimi
                                    if (!offer.Name.Trim().Contains("Taahhütsüz") && !offer.Name.Trim().Contains("TK"))
                                    {
                                        responseData.Add(new OTFModel<decimal>
                                        {
                                            Default = true,
                                            OTFId = Guid.Parse("6bf2d30a-1e97-4f68-a2d2-ee2442c906cf"),
                                            OTFType = OTFTypeEnum.TTActivationDiscount,
                                            Price = new PriceModel<decimal>
                                            {
                                                Price = decimal.Parse("-" + price.ToString()),
                                                Duration = new Common.Models.DurationModel
                                                {
                                                    Duration = offer.Term.Duration,
                                                    DurationType = offer.Term.DurationType
                                                },
                                                DurationStart = new Common.Models.DurationModel
                                                {
                                                    Duration = 0,
                                                    DurationType = DurationTypeEnum.Second
                                                }
                                            },
                                            InstallmentCount=0
                                        });
                                    }

                                }
                            }

                        }
                    }

                    return Ok(new ApiResponse<List<OTFModel<decimal>>>(responseData));
                }
                catch (Exception ex)
                {
                    return Ok(new ApiResponse(ex.Message));
                }
            }
            else
            {
                return Ok(new ApiResponse("There is no any Product or ProductOffer with the ids!"));
            }
        }

    }
}
