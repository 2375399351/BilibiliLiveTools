using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Bilibili.AspNetCore.Apis.Interface;
using Bilibili.AspNetCore.Apis.Models;
using Bilibili.AspNetCore.Apis.Models.Base;
using BilibiliAutoLiver.Config;
using BilibiliAutoLiver.Models.Dtos;
using BilibiliAutoLiver.Models.Entities;
using BilibiliAutoLiver.Models.ViewModels;
using BilibiliAutoLiver.Repository.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace BilibiliAutoLiver.Controllers
{
    [Authorize]
    public class RoomController : Controller
    {
        private readonly ILogger<RoomController> _logger;
        private readonly IMemoryCache _cache;
        private readonly IBilibiliAccountApiService _accountService;
        private readonly IBilibiliCookieService _cookieService;
        private readonly IBilibiliLiveApiService _liveApiService;
        private readonly ILiveSettingRepository _repository;

        public RoomController(ILogger<RoomController> logger
            , IMemoryCache cache
            , IBilibiliAccountApiService accountService
            , IBilibiliCookieService cookieService
            , IBilibiliLiveApiService liveApiService
            , ILiveSettingRepository repository)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _accountService = accountService ?? throw new ArgumentNullException(nameof(accountService));
            _cookieService = cookieService ?? throw new ArgumentNullException(nameof(cookieService));
            _liveApiService = liveApiService ?? throw new ArgumentNullException(nameof(liveApiService));
            _repository = repository ?? throw new ArgumentNullException(nameof(repository));
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            MyLiveRoomInfo myLiveRoomInfo = await _liveApiService.GetMyLiveRoomInfo();
            List<LiveAreaItem> liveAreas = await _liveApiService.GetLiveAreas();
            LiveSetting liveSetting = await _repository.Where(p => !p.IsDeleted).OrderByDescending(p => p.CreatedTime).FirstAsync();

            RoomInfoIndexPageViewModel viewModel = new RoomInfoIndexPageViewModel()
            {
                LiveRoomInfo = myLiveRoomInfo,
                LiveAreas = liveAreas,
                LiveSetting = liveSetting
            };

            return View(viewModel);
        }

        [HttpPost]
        public async Task<ResultModel<string>> Update([FromBody] RoomInfoUpdateRequest request)
        {
            try
            {
                if (!ModelState.IsValid)
                {
                    return new ResultModel<string>(-1, "��������");
                }
                //����Bվapi���ȸ���bվֱ����Ϣ
                LiveSetting liveSetting = await _repository.Where(p => !p.IsDeleted).OrderByDescending(p => p.CreatedTime).FirstAsync();
                if (liveSetting == null || liveSetting.AreaId != request.AreaId)
                {
                    //���·���
                    if (!await _liveApiService.UpdateLiveRoomArea(request.RoomId, request.AreaId))
                    {
                        return new ResultModel<string>(-1, "����Bilibiliֱ������ʧ��");
                    }
                }
                if (liveSetting == null || liveSetting.RoomName != request.RoomName)
                {
                    //����ֱ������
                    if (!await _liveApiService.UpdateLiveRoomName(request.RoomId, request.RoomName))
                    {
                        return new ResultModel<string>(-1, "����Bilibiliֱ������ʧ��");
                    }
                }
                if (liveSetting == null)
                {
                    liveSetting = new LiveSetting()
                    {
                        CreatedTime = DateTime.UtcNow,
                        CreatedUserId = GlobalConfigConstant.SYS_USERID,
                    };
                }
                liveSetting.AreaId = request.AreaId;
                liveSetting.RoomName = request.RoomName;
                liveSetting.IsAutoRetry = request.IsAutoRetry;
                liveSetting.RetryInterval = request.RetryInterval;
                liveSetting.UpdatedTime = DateTime.UtcNow;
                liveSetting.UpdatedUserId = GlobalConfigConstant.SYS_USERID;

                await _repository.InsertOrUpdateAsync(liveSetting);
                return new ResultModel<string>(0);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"����ֱ������Ϣʧ�ܣ�{ex.Message}");
                return new ResultModel<string>(-1, ex.Message);
            }
        }

        /// <summary>
        /// ����������ϢΪMarkdown��ʽ
        /// </summary>
        /// <returns></returns>
        /// <exception cref="Exception"></exception>
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ExportAreas()
        {
            List<LiveAreaItem> info = await _liveApiService.GetLiveAreas();
            if (info == null || info.Count == 0)
            {
                throw new Exception("��ȡֱ������ʧ�ܡ�");
            }

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("### ֱ���������Ϣ");
            sb.AppendLine();
            sb.AppendLine("|  AreaId | ��������  | ��������  |");
            sb.AppendLine("| :------------ | :------------ | :------------ |");
            foreach (var bigCate in info)
            {
                foreach (var item in bigCate.list)
                {
                    sb.AppendLine($" | {item.id} | {item.name} | {item.parent_name} | ");
                }
            }
            return File(Encoding.UTF8.GetBytes(sb.ToString()), "text/markdown; charset=UTF-8", "AreasInfo.md");
        }
    }
}
