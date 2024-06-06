using System;
using System.Linq;
using System.Threading.Tasks;
using Bilibili.AspNetCore.Apis.Interface;
using Bilibili.AspNetCore.Apis.Models.Base;
using BilibiliAutoLiver.Config;
using BilibiliAutoLiver.Models.Dtos;
using BilibiliAutoLiver.Models.Entities;
using BilibiliAutoLiver.Models.Enums;
using BilibiliAutoLiver.Models.ViewModels;
using BilibiliAutoLiver.Repository.Interface;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;

namespace BilibiliAutoLiver.Controllers
{
    [Authorize]
    public class PushController : Controller
    {
        private readonly ILogger<PushController> _logger;
        private readonly IMemoryCache _cache;
        private readonly IBilibiliAccountApiService _accountService;
        private readonly IBilibiliCookieService _cookieService;
        private readonly IBilibiliLiveApiService _liveApiService;
        private readonly IPushSettingRepository _pushSettingRepository;

        public PushController(ILogger<PushController> logger
            , IMemoryCache cache
            , IBilibiliAccountApiService accountService
            , IBilibiliCookieService cookieService
            , IBilibiliLiveApiService liveApiService
            , ILiveSettingRepository liveSettingRepos
            , IPushSettingRepository pushSettingRepository)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _cache = cache ?? throw new ArgumentNullException(nameof(cache));
            _accountService = accountService ?? throw new ArgumentNullException(nameof(accountService));
            _cookieService = cookieService ?? throw new ArgumentNullException(nameof(cookieService));
            _liveApiService = liveApiService ?? throw new ArgumentNullException(nameof(liveApiService));
            _pushSettingRepository = pushSettingRepository ?? throw new ArgumentNullException(nameof(pushSettingRepository));
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            PushIndexPageViewModel vm = new PushIndexPageViewModel();
            vm.PushSetting = await _pushSettingRepository.Where(p => !p.IsDeleted).FirstAsync();
            if (vm.PushSetting == null)
            {
                throw new Exception("��ȡ��������ʧ�ܣ�");
            }
            return View(vm);
        }

        public async Task<ResultModel<string>> Update([FromBody] PushSettingUpdateRequest request)
        {
            PushSetting setting = await _pushSettingRepository.Where(p => !p.IsDeleted).FirstAsync();
            if (setting == null)
            {
                throw new Exception("��ȡ��������ʧ�ܣ�");
            }
            ResultModel<string> modelUpdateResult = null;
            switch (request.Model)
            {
                case ConfigModel.Easy:
                    modelUpdateResult = UpdateEasyModel(request, setting);
                    break;
                case ConfigModel.Advance:
                    modelUpdateResult = UpdateAdvanceModel(request, setting);
                    break;
                default:
                    throw new NotSupportedException("��������δ֪����������");
            }
            if (modelUpdateResult.Code != 0)
            {
                return modelUpdateResult;
            }
            //������������
            setting.IsAutoRetry = request.IsAutoRetry;
            setting.RetryInterval = request.RetryInterval;
            setting.UpdatedTime = DateTime.UtcNow;
            setting.UpdatedUserId = GlobalConfigConstant.SYS_USERID;
            setting.IsUpdate = true;

            int updateResult = await _pushSettingRepository.UpdateAsync(setting);
            if (updateResult <= 0)
            {
                throw new Exception("����������Ϣʧ�ܣ�");
            }
            return new ResultModel<string>(0);
        }

        private ResultModel<string> UpdateAdvanceModel(PushSettingUpdateRequest request, PushSetting setting)
        {
            if (setting.Model == ConfigModel.Easy)
            {
                setting.Model = ConfigModel.Advance;
            }
            if (string.IsNullOrWhiteSpace(setting.FFmpegCommand))
            {
                return new ResultModel<string>(-1, "���������Ϊ��");
            }
            //������������ 
            var cmdLines = request.FFmpegCommand
                .ReplaceLineEndings()
                .Split(Environment.NewLine)
                .Where(p => !string.IsNullOrWhiteSpace(p))
                .Select(p => p.Trim(' ', '\r', '\n'))
                .Where(p => !string.IsNullOrWhiteSpace(p) && !p.StartsWith("//"))
                .ToList();
            if (!cmdLines.Any())
            {
                return new ResultModel<string>(-1, "���벻�淶�����������ᣨû���ҵ�ffmpeg��ͷ������[[URL]]��β���������");
            }
            var ffmpegCmdLines = cmdLines.Where(p => p.StartsWith("ffmpeg", StringComparison.OrdinalIgnoreCase) && p.EndsWith("[[URL]]")).ToArray();
            if (ffmpegCmdLines.Length == 0)
            {
                return new ResultModel<string>(-1, "���벻�淶�����������ᣨû���ҵ�ffmpeg��ͷ������[[URL]]��β���������");
            }
            if (ffmpegCmdLines.Length > 1)
            {
                return new ResultModel<string>(-1, "���벻�淶�����������ᣨ���ڶ���ffmpeg�����ע�Ͳ���Ҫ���������");
            }
            if (cmdLines.Count(p => !p.StartsWith("//") && !(p.StartsWith("ffmpeg", StringComparison.OrdinalIgnoreCase) && p.EndsWith("[[URL]]"))) >= 1)
            {
                return new ResultModel<string>(-1, "���벻�淶�����������ᣨ�����޷���������������á�//������ע�ͣ�");
            }
            setting.FFmpegCommand = request.FFmpegCommand;
            return new ResultModel<string>(0);
        }

        private ResultModel<string> UpdateEasyModel(PushSettingUpdateRequest request, PushSetting setting)
        {
            if (setting.Model == ConfigModel.Advance)
            {
                setting.Model = ConfigModel.Easy;
            }
            return new ResultModel<string>(0);
        }

    }
}
