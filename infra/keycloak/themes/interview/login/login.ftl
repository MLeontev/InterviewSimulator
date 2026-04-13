<#import "template.ftl" as layout>

<@layout.registrationLayout displayMessage=false displayInfo=false; section>
  <#if section == "header">
    Вход
  <#elseif section == "form">

    <div class="is-auth-page">
      <main class="is-auth-main">
        <div class="is-auth-card">
          <h1 class="is-auth-title">Вход</h1>

          <form id="kc-form-login" onsubmit="login.disabled = true; return true;" action="${url.loginAction}" method="post">
            <#assign loginError = ''>
            <#if messagesPerField.existsError('username','password')>
              <#assign loginError = messagesPerField.getFirstError('username','password')>
            <#elseif message?has_content && message.type == 'error'>
              <#assign loginError = message.summary>
            </#if>

            <#if !usernameHidden??>
              <div class="is-field">
                <label for="username" class="is-label">Email или логин</label>
                <input
                  tabindex="1"
                  id="username"
                  class="is-input"
                  name="username"
                  value="${(login.username!'')}"
                  type="text"
                  autofocus
                  autocomplete="username"
                />
              </div>
            <#else>
              <input id="username" name="username" type="hidden" value="${(login.username!'')}" />
            </#if>

            <div class="is-field">
              <label for="password" class="is-label">Пароль</label>
              <input
                tabindex="2"
                id="password"
                class="is-input"
                name="password"
                type="password"
                autocomplete="current-password"
              />
              <#if loginError?has_content>
                <div class="is-field-error">${kcSanitize(loginError)?no_esc}</div>
              </#if>
            </div>

            <#if (realm.rememberMe && !usernameHidden??) || realm.resetPasswordAllowed>
              <div class="is-row">
                <#if realm.rememberMe && !usernameHidden??>
                  <label class="is-checkbox">
                    <input tabindex="3" id="rememberMe" name="rememberMe" type="checkbox" <#if login.rememberMe??>checked</#if> />
                    <span>Запомнить меня</span>
                  </label>
                </#if>

                <#if realm.resetPasswordAllowed>
                  <a class="is-link" href="${url.loginResetCredentialsUrl}">
                    Забыли пароль?
                  </a>
                </#if>
              </div>
            </#if>

            <#if realm.password && credentialSelection??>
              <input type="hidden" name="credentialId" value="${credentialSelection.selectedCredentialId}" />
            </#if>

            <button tabindex="4" class="is-btn is-btn-primary" name="login" id="kc-login" type="submit">
              Войти
            </button>
          </form>

          <#assign extReg = properties.registrationExternalUrl!''>
          <#if extReg?has_content>
            <div class="is-auth-alt">
              Нет аккаунта?
              <a class="is-link-inline" href="${extReg}">Зарегистрироваться</a>
            </div>
          <#elseif realm.password && realm.registrationAllowed>
            <div class="is-auth-alt">
              Нет аккаунта?
              <a class="is-link-inline" href="${url.registrationUrl}">Зарегистрироваться</a>
            </div>
          </#if>
        </div>
      </main>

      <footer class="is-auth-footer">
        <div class="is-auth-footer-inner">© 2026 Тренажер собеседований</div>
      </footer>
    </div>

  </#if>
</@layout.registrationLayout>
