function createRegisterFormBase(registerUrl, loginUrl) {
    var form = "        <form id=\"register-form\" role=\"form\" action=\"" + registerUrl + "\" method=\"POST\">" +
        "            <div>" +
        "                <h4>Ny her?</h4>" +
        "                Registrer deg, og få tilgang til enda mer." +
        "            </div>" +
        "            <div class=\"form-group\">" +
        "                <label for=\"DisplayName\">Navn</label>" +
        "                <input type=\"text\" class=\"form-control\" id=\"DisplayName\" name=\"DisplayName\" placeholder=\"Fornavn Etternavn\">" +
        "                <span class=\"help-block\"></span>" +
        "            </div>" +
        "            <div class=\"form-group\">" +
        "                <label for=\"UserName\">Brukernavn</label>" +
        "                <input type=\"text\" class=\"form-control\" id=\"UserName\" name=\"UserName\" placeholder=\"Velg et brukernavn\">" +
        "                <span class=\"help-block\"></span>" +
        "            </div>" +
        "            <div class=\"form-group\">" +
        "                <label for=\"Email\">E-post</label>" +
        "                <input type=\"email\" class=\"form-control\" id=\"Email\" name=\"Email\" placeholder=\"E-postadressen din\">" +
        "                <span class=\"help-block\"></span>" +
        "            </div>" +
        "            <div class=\"form-group\">" +
        "                <label for=\"Password\">Passord</label>" +
        "                <input type=\"password\" class=\"form-control\" id=\"Password\" name=\"Password\" placeholder=\"Velg et passord\">" +
        "                <span class=\"help-block\"></span>" +
        "            </div>" +
        "            <div class=\"form-group\">" +
        "                <label for=\"RepeatedPassword\">Gjenta passordet</label>" +
        "                <input type=\"password\" class=\"form-control\" id=\"RepeatedPassword\" name=\"RepeatedPassword\" placeholder=\"Gjenta passordet\">" +
        "                <span class=\"help-block\"></span>" +
        "            </div>" +
        "            <div>" +
        "                <button id=\"register\" class=\"btn btn-default\">Registrer</button>" +
        "                <button id=\"switch-to-login\" class=\"btn-link\">Logg inn</button>" +
        "            </div>" +
        "        </form>";
    $("#auth").html(form);
    $("#switch-to-login").click(function (e) {
        e.preventDefault();
        createLoginFormBase(registerUrl, loginUrl);
    });

    $("#register-form").bindForm({
        validate: function () {
            var params = $(this).serializeMap();
            if (params.RepeatedPassword !== params.Password) {
                $(this).setFieldError("Password", "Passordene stemmer ikke overens");
                return false;
            }
            return true;
        },
        success: function (user) {
            console.log('register-form: ' + user.userId);
            var params = $("#register-form").serializeMap();
            createLoginFormBase(registerUrl, loginUrl);
            $("#UserName").val(params.UserName);
            $("#Password").val(params.Password);
        }
    });
}

function createLoginFormBase(registerUrl, loginUrl) {
    var value = "";
    try {
        if (window.localStorage["userName"]) {
            var userName = window.localStorage["userName"];
            value = " value=\"" + userName + "\"";
        }
    } catch (exception) {}

    var form = "        <form id=\"login-form\" class=\"login\" role=\"form\" action=\"" + loginUrl + "\" method=\"POST\">" +
        "            <div>" +
        "                <h4>Logg inn</h4>" +
        "            </div>" +
        "            <div class=\"form-group\">" +
        "                <label for=\"UserName\">Brukernavn</label>" +
        "                <input type=\"text\" class=\"form-control\" id=\"UserName\" name=\"UserName\" placeholder=\"Brukernavn\"" + value + ">" +
        "                <span class=\"help-block\"></span>" +
        "            </div>" +
        "            <div class=\"form-group\">" +
        "                <label for=\"Password\">Passord</label>" +
        "                <input type=\"password\" class=\"form-control\" id=\"Password\" name=\"Password\" placeholder=\"Passord\">" +
        "                <span class=\"help-block\"></span>" +
        "            </div>" +
        "            <div class=\"checkbox\">" +
        "                <label>" +
        "                    <input type=\"checkbox\" id=\"RememberMe\" name=\"RememberMe\"> Husk innlogging" +
        "                </label>" +
        "            </div>" +
        "            <div>" +
        "                <button id=\"login\" class=\"btn btn-default\">Logg inn</button>" +
        "                <button id=\"switch-to-register\" class=\"btn-link\">Registrer bruker</button>" +
        "" +
        "                <button id=\"forgot\" class=\"btn-link\">Glemt passord?</button>" +
        "            </div>" +
        "        </form>";

    $("#auth").html(form);

    $("#switch-to-register").click(function (e) {
        e.preventDefault();
        createRegisterFormBase(registerUrl, loginUrl);
    });

    $("#login-form").bindForm({
        success: function (authenticateResponse) {
            var params = $("#login-form").serializeMap();
            try {
                window.localStorage["userName"] = params.UserName;
            } catch (exception) {}
            location.reload(true);
        }
    });
}



