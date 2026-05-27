function createRegisterFormBase(registerUrl, loginUrl) {
    var html =
        '<form id="register-form" role="form" action="' + registerUrl + '" method="POST">' +
            '<div><h4>Ny her?</h4>Registrer deg, og få tilgang til enda mer.</div>' +
            '<div class="form-group">' +
                '<label for="DisplayName">Navn</label>' +
                '<input type="text" class="form-control" id="DisplayName" name="DisplayName" placeholder="Fornavn Etternavn">' +
                '<span class="help-block"></span>' +
            '</div>' +
            '<div class="form-group">' +
                '<label for="Email">E-post</label>' +
                '<input type="email" class="form-control" id="Email" name="Email" placeholder="E-postadressen din">' +
                '<span class="help-block"></span>' +
            '</div>' +
            '<div class="form-group">' +
                '<label for="Password">Passord</label>' +
                '<input type="password" class="form-control" id="Password" name="Password" placeholder="Velg et passord">' +
                '<span class="help-block"></span>' +
            '</div>' +
            '<div class="form-group">' +
                '<label for="ConfirmPassword">Gjenta passordet</label>' +
                '<input type="password" class="form-control" id="ConfirmPassword" name="ConfirmPassword" placeholder="Gjenta passordet">' +
                '<span class="help-block"></span>' +
            '</div>' +
            '<div>' +
                '<button id="register" type="submit" class="btn btn-default">Registrer</button>' +
                '<button id="switch-to-login" type="button" class="btn-link">Logg inn</button>' +
            '</div>' +
        '</form>';

    $("#auth").html(html);

    $("#switch-to-login").click(function (e) {
        e.preventDefault();
        createLoginFormBase(registerUrl, loginUrl);
    });

    $("#register-form").on('submit', function (e) {
        e.preventDefault();
        var form = $(this);
        clearFormErrors(form);

        var values = serializeFields(form);
        if (values.Password !== values.ConfirmPassword) {
            setFieldError(form, "ConfirmPassword", "Passordene stemmer ikke overens");
            return;
        }

        $.ajax({
            type: 'POST',
            url: form.attr('action'),
            data: form.serialize(),
            headers: { 'X-Requested-With': 'XMLHttpRequest' },
            dataType: 'json'
        }).done(function () {
            createLoginFormBase(registerUrl, loginUrl);
            $("#UserName").val(values.Email);
            $("#Password").val(values.Password);
        }).fail(function (xhr) {
            applyServerErrors(form, xhr);
        });
    });
}

function createLoginFormBase(registerUrl, loginUrl) {
    var rememberedValue = "";
    try {
        if (window.localStorage["userName"]) {
            rememberedValue = ' value="' + window.localStorage["userName"] + '"';
        }
    } catch (exception) { /* localStorage unavailable */ }

    var html =
        '<form id="login-form" class="login" role="form" action="' + loginUrl + '" method="POST">' +
            '<div><h4>Logg inn</h4></div>' +
            '<div class="form-group">' +
                '<label for="UserName">Brukernavn</label>' +
                '<input type="text" class="form-control" id="UserName" name="UserName" placeholder="Brukernavn"' + rememberedValue + '>' +
                '<span class="help-block"></span>' +
            '</div>' +
            '<div class="form-group">' +
                '<label for="Password">Passord</label>' +
                '<input type="password" class="form-control" id="Password" name="Password" placeholder="Passord">' +
                '<span class="help-block"></span>' +
            '</div>' +
            '<div class="checkbox">' +
                '<label>' +
                    '<input type="checkbox" id="RememberMe" name="RememberMe" value="true"> Husk innlogging' +
                '</label>' +
            '</div>' +
            '<div>' +
                '<button id="login" type="submit" class="btn btn-default">Logg inn</button>' +
                '<button id="switch-to-register" type="button" class="btn-link">Registrer bruker</button>' +
                '<button id="forgot" type="button" class="btn-link">Glemt passord?</button>' +
            '</div>' +
        '</form>';

    $("#auth").html(html);

    $("#switch-to-register").click(function (e) {
        e.preventDefault();
        createRegisterFormBase(registerUrl, loginUrl);
    });

    $("#login-form").on('submit', function (e) {
        e.preventDefault();
        var form = $(this);
        clearFormErrors(form);

        var values = serializeFields(form);

        $.ajax({
            type: 'POST',
            url: form.attr('action'),
            data: form.serialize(),
            headers: { 'X-Requested-With': 'XMLHttpRequest' },
            dataType: 'json'
        }).done(function () {
            try {
                window.localStorage["userName"] = values.UserName;
            } catch (exception) { /* localStorage unavailable */ }
            location.reload();
        }).fail(function (xhr) {
            applyServerErrors(form, xhr);
        });
    });
}

function serializeFields(form) {
    var values = {};
    $.each(form.serializeArray(), function (_, field) {
        values[field.name] = field.value;
    });
    return values;
}

function clearFormErrors(form) {
    form.find(".form-group").removeClass("has-error");
    form.find(".help-block").text("");
    form.find(".form-error").remove();
}

function setFieldError(form, fieldName, message) {
    var input = form.find('[name="' + fieldName + '"]');
    input.closest(".form-group").addClass("has-error");
    input.siblings(".help-block").text(message);
}

function setFormError(form, message) {
    var alert = form.find(".form-error");
    if (alert.length === 0) {
        alert = $('<div class="alert alert-danger form-error"></div>').prependTo(form);
    }
    alert.text(message);
}

function applyServerErrors(form, xhr) {
    var body = xhr.responseJSON;
    if (body && body.errors) {
        $.each(body.errors, function (fieldName, messages) {
            var text = [].concat(messages).join(" ");
            if (fieldName) {
                setFieldError(form, fieldName, text);
            } else {
                setFormError(form, text);
            }
        });
    } else if (body && body.error) {
        setFormError(form, body.error);
    } else {
        setFormError(form, "Noe gikk galt. Prøv igjen.");
    }
}
