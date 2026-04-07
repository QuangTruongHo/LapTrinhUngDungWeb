/**
 * Hiển thị ảnh được chọn từ input file lên thẻ img
 * (Thẻ input có thuộc tính data-img-preview trỏ đến id của thẻ img dùng để hiển thị ảnh)
 */
function previewImage(input) {
    if (!input.files || !input.files[0]) return;

    const previewId = input.dataset.imgPreview; // lấy data-img-preview từ attribute
    if (!previewId) return;

    const img = document.getElementById(previewId);
    if (!img) return;

    const reader = new FileReader();
    reader.onload = function (e) {
        img.src = e.target.result;
    };
    reader.readAsDataURL(input.files[0]);
}

/**
 * Tìm kiếm phân trang bằng AJAX và cập nhật thanh địa chỉ (URL)
 */
function paginationSearch(event, form, page) {
    if (event) event.preventDefault();
    if (!form) return;

    const url = form.action;
    const method = (form.method || "GET").toUpperCase();
    const targetId = form.dataset.target;

    const formData = new FormData(form);
    // Sử dụng set để đảm bảo tham số 'page' là duy nhất và cập nhật giá trị mới nhất
    formData.set("page", page);

    let fetchUrl = url;
    if (method === "GET") {
        const params = new URLSearchParams(formData).toString();
        fetchUrl = url + "?" + params;

        // --- PHẦN SỬA ĐỔI: Cập nhật thanh địa chỉ trình duyệt ---
        // Giúp lưu lại trạng thái tìm kiếm khi người dùng F5 hoặc copy link
        window.history.pushState(null, "", fetchUrl);
    }

    let targetEl = null;
    if (targetId) {
        targetEl = document.getElementById(targetId);
        if (targetEl) {
            targetEl.innerHTML = `
                <div class="text-center py-4">
                    <div class="spinner-border text-primary" role="status"></div>
                    <div class="mt-2">Đang tải dữ liệu...</div>
                </div>`;
        }
    }

    fetch(fetchUrl, {
        method: method,
        body: method === "GET" ? null : formData
    })
        .then(res => {
            if (!res.ok) throw new Error("Network response was not ok");
            return res.text();
        })
        .then(html => {
            if (targetEl) {
                targetEl.innerHTML = html;
            }
        })
        .catch((error) => {
            console.error("Search Error:", error);
            if (targetEl) {
                targetEl.innerHTML = `
                <div class="alert alert-danger mt-3">
                    <i class="bi bi-exclamation-triangle-fill"></i> Không tải được dữ liệu. Vui lòng thử lại sau.
                </div>`;
            }
        });
}

/**
 * Mở modal và load nội dung từ link vào modal bằng AJAX
 */
(function () {
    // dialogModal là id của modal dùng chung được định nghĩa trong _Layout.cshtml
    const modalEl = document.getElementById("dialogModal");
    if (!modalEl) return;

    const modalContent = modalEl.querySelector(".modal-content");

    // Clear nội dung khi modal đóng để tránh lộ dữ liệu cũ của lần mở trước
    modalEl.addEventListener('hidden.bs.modal', function () {
        modalContent.innerHTML = '';
    });

    window.openModal = function (event, link) {
        if (!link) return;
        if (event) event.preventDefault();

        const url = link.getAttribute("href");

        // Hiển thị trạng thái loading bên trong modal
        modalContent.innerHTML = `
            <div class="modal-body text-center py-5">
                <div class="spinner-border text-info" role="status"></div>
                <div class="mt-2">Đang tải dữ liệu...</div>
            </div>`;

        // Khởi tạo hoặc lấy instance hiện tại của Bootstrap Modal
        let modal = bootstrap.Modal.getInstance(modalEl);
        if (!modal) {
            modal = new bootstrap.Modal(modalEl, {
                backdrop: 'static',
                keyboard: false
            });
        }

        modal.show();

        // Load nội dung HTML từ Server
        fetch(url)
            .then(res => {
                if (!res.ok) throw new Error("Modal load failed");
                return res.text();
            })
            .then(html => {
                modalContent.innerHTML = html;
            })
            .catch((error) => {
                console.error("Modal Error:", error);
                modalContent.innerHTML = `
                    <div class="modal-body text-center text-danger py-5">
                        <i class="bi bi-x-circle" style="font-size: 2rem;"></i>
                        <p class="mt-2">Không tải được nội dung yêu cầu.</p>
                        <button type="button" class="btn btn-secondary btn-sm" data-bs-dismiss="modal">Đóng</button>
                    </div>`;
            });
    };
})();