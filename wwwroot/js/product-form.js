// product-form.js — tách ra khỏi Razor để tránh parse lỗi ký tự đặc biệt

var excludeId = 0; // được set từ view qua window.productExcludeId
var nameInput = null;
var nameStatus = null;
var nameTimer = null;

function hasSpecialChar(str) {
    var bad = ['<', '>', '{', '}', '[', ']', '\\', '|', '*', '?', '!', '@',
        '#', '$', '^', '&', '+', '=', '~', '`'];
    for (var i = 0; i < bad.length; i++) {
        if (str.indexOf(bad[i]) !== -1) return true;
    }
    return false;
}

function initProductForm(productExcludeId) {
    excludeId = productExcludeId || 0;
    nameInput = document.getElementById('productName');
    nameStatus = document.getElementById('nameStatus');
    if (!nameInput) return;

    nameInput.addEventListener('input', function () {
        clearTimeout(nameTimer);
        var val = nameInput.value.trim();
        nameStatus.textContent = '';
        nameStatus.className = '';
        if (!val) return;

        if (hasSpecialChar(val)) {
            nameStatus.textContent = '\u2717 Tên không được chứa ký tự đặc biệt.';
            nameStatus.className = 'name-dup';
            return;
        }

        nameTimer = setTimeout(function () {
            fetch('/Admin/Product/CheckName?name=' + encodeURIComponent(val) + '&excludeId=' + excludeId)
                .then(function (r) { return r.json(); })
                .then(function (data) {
                    if (data.exists) {
                        nameStatus.textContent = '\u2717 Tên sản phẩm này đã tồn tại.';
                        nameStatus.className = 'name-dup';
                    } else {
                        nameStatus.textContent = '\u2713 Tên hợp lệ.';
                        nameStatus.className = 'name-ok';
                    }
                })
                .catch(function () { });
        }, 400);
    });
}

function validateAndPreview(input) {
    var fileErr = document.getElementById('fileErr');
    fileErr.style.display = 'none';
    if (!input.files || !input.files[0]) return;
    var file = input.files[0];
    var allowed = ['.jpg', '.jpeg', '.png', '.gif', '.webp'];
    var ext = '.' + file.name.split('.').pop().toLowerCase();

    if (allowed.indexOf(ext) === -1) {
        fileErr.textContent = '\u2717 File "' + file.name + '" không được phép. Chỉ chấp nhận ảnh.';
        fileErr.style.display = 'block';
        input.value = '';
        return;
    }
    if (file.size > 5 * 1024 * 1024) {
        fileErr.textContent = '\u2717 File vượt quá 5MB.';
        fileErr.style.display = 'block';
        input.value = '';
        return;
    }
    var reader = new FileReader();
    reader.onload = function (e) {
        document.getElementById('imgPreview').src = e.target.result;
    };
    reader.readAsDataURL(file);
}