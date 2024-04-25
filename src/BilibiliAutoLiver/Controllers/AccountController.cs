using System;
using System.Threading.Tasks;
using Bilibili.AspNetCore.Apis.Interface;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace BilibiliAutoLiver.Controllers
{
    public class AccountController : Controller
    {
        private readonly ILogger<AccountController> _logger;
        private readonly IBilibiliAccountApiService _accountService;
        private readonly IBilibiliCookieService _cookieService;

        public AccountController(ILogger<AccountController> logger
            , IBilibiliAccountApiService accountService
            , IBilibiliCookieService cookieService)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _accountService = accountService ?? throw new ArgumentNullException(nameof(accountService));
            _cookieService = cookieService ?? throw new ArgumentNullException(nameof(cookieService));
        }

        /// <summary>
        /// ˢ��Cookie
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> Refresh()
        {
            try
            {
                await _accountService.RefreshCookie();
                _logger.LogInformation("ǿ������ˢ��Cookie�ɹ���");
                return Content("ˢ�³ɹ�");
            }
            catch (Exception ex)
            {
                return Content(ex.Message);
            }
        }

        /// <summary>
        /// ���µ�¼
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> ReLogin()
        {
            await _cookieService.RemoveCookie();
            _ = _accountService.LoginByQrCode();

            return Content("����ɨ���¼���뷵����ҳ����ɨ���¼��");
        }
    }
}
