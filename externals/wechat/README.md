# 微信第三方平台

## 相关概念

- 第三方平台应用 `component_appid`
- 第三方平台口令 `component_appsecret`
- 加解密钥 `symmetric_key`, `AESEncodingKey`

- 平台访问令牌 `component_access_token`
  > 第三方平台接口的调用凭据，令牌的有效期为2小时，在令牌快过期时（比如1小时50分），需调用接口重新获取。
  > 参考文档：https://developers.weixin.qq.com/doc/oplatform/Third-party_Platforms/api/component_access_token.html

- 平台验证票据 `component_verify_ticket`
  > 当第三方平台创建审核通过后，微信服务器会向其“授权事件接收URL”每隔10分钟以`POST`的方式推送验证票据，第三方平台接收到该请求后，只需直接返回字符串`success`即可。
  > 参考文档：https://developers.weixin.qq.com/doc/oplatform/Third-party_Platforms/api/component_verify_ticket.html

## 授权流程
  1. 坐等微信推送的票据验证回调 `component_verify_ticket`
  2. 获取或异步更新平台访问令牌 `component_access_token`
  3. 通过平台访问令牌来预授权码 `pre_auth_code`
  4. 根据预授权码来生成两种授权页面地址
     1. 扫描页面：https://mp.weixin.qq.com/cgi-bin/componentloginpage?component_appid=xxxx&pre_auth_code=xxxxx&redirect_uri=xxxx&auth_type=xxx
     2. 微信分享：https://mp.weixin.qq.com/safe/bindcomponent?action=bindcomponent&no_scan=1&component_appid=xxxx&pre_auth_code=xxxxx&redirect_uri=xxxx&auth_type=xxx&biz_appid=xxxx#wechat_redirect
  5. 用户授权完成，微信推送回调，在回调中获取授权绑定信息
   > 通过 https://developers.weixin.qq.com/doc/oplatform/Third-party_Platforms/api/authorization_info.html 获取授权者的 `authorizer_appid`, `authorizer_access_token`, `authorizer_refresh_token`
  6. 定时更新授权者的访问凭证
