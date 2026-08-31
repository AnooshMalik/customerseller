var cart = JSON.parse(localStorage.getItem("cart")) || [];

function saveCart() {
    localStorage.setItem("cart", JSON.stringify(cart));
    updateCartCount();
}

function updateCartCount() {
    var count = cart.reduce(function (total, item) {
        return total + item.qty;
    }, 0);
    var badge = document.getElementById("cartCount");
    if (!badge) return;
    if (count > 0) {
        badge.textContent = count;
        badge.style.display = "block";
    } else {
        badge.style.display = "none";
    }
}

function renderCart() {
    var cartItems = document.getElementById("cartItems");
    var cartTotal = document.getElementById("cartTotal");
    var emptyCart = document.getElementById("emptyCart");

    if (!cartItems) return;

    cartItems.innerHTML = "";

    if (cart.length === 0) {
        if (emptyCart) emptyCart.style.display = "flex";
        if (cartTotal) cartTotal.innerHTML = "";
        return;
    }

    if (emptyCart) emptyCart.style.display = "none";

    var total = 0;
    cart.forEach(function (item) {
        total += item.price * item.qty;
        cartItems.innerHTML += '<div style="display:flex;align-items:flex-start;gap:18px;margin-bottom:25px;padding-bottom:18px;border-bottom:1px solid #ececec;">' +
            '<img src="' + item.img + '" alt="' + item.title + '" style="width:74px;height:74px;border-radius:10px;object-fit:cover;border:1.5px solid #ececec;">' +
            '<div class="cart-item-info" style="flex:1;display:flex;flex-direction:column;align-items:flex-start;">' +
            '<div class="cart-item-title">' + item.title + '</div>' +
            '<div class="cart-item-price">Rs.' + item.price + ' x ' + item.qty + '</div>' +
            '<div class="qty-controls" style="display:flex;align-items:center;gap:10px;">' +
            '<button class="qty-btn" onclick="changeQty(\'' + item.id + '\', -1)">-</button>' +
            '<span>' + item.qty + '</span>' +
            '<button class="qty-btn" onclick="changeQty(\'' + item.id + '\', 1)">+</button>' +
            '<button class="cart-delete-btn" onclick="removeItem(\'' + item.id + '\')">&#128465;</button>' +
            '</div></div></div>';
    });

    if (cartTotal) cartTotal.innerHTML = 'Subtotal: <strong>Rs.' + total.toLocaleString() + '</strong>';
    saveCart();
}

function changeQty(id, delta) {
    var item = cart.find(function (i) { return i.id === id; });
    if (!item) return;
    item.qty += delta;
    if (item.qty <= 0) cart = cart.filter(function (i) { return i.id !== id; });
    renderCart();
    updateCartCount();
}

function removeItem(id) {
    cart = cart.filter(function (i) { return i.id !== id; });
    renderCart();
    updateCartCount();
}

function openCart() {
    var overlay = document.getElementById("cartOverlay");
    var sidebar = document.getElementById("cartSidebar");
    if (overlay) overlay.classList.add("active");
    if (sidebar) sidebar.classList.add("active");
    document.body.style.overflow = "hidden";
    renderCart();
    updateCartCount();
}

function closeCart() {
    var overlay = document.getElementById("cartOverlay");
    var sidebar = document.getElementById("cartSidebar");
    if (overlay) overlay.classList.remove("active");
    if (sidebar) sidebar.classList.remove("active");
    document.body.style.overflow = "";
}

document.addEventListener("click", function (e) {
    var plusBtn = e.target.closest(".plus-btn, .pcard-plus");
    if (!plusBtn) return;

    e.preventDefault();
    e.stopPropagation();

    var id = plusBtn.getAttribute("data-id");
    var title = plusBtn.getAttribute("data-title");
    var price = Number(plusBtn.getAttribute("data-price"));
    var img = plusBtn.getAttribute("data-img");

    if (!id || !title || !price || isNaN(price) || !img) return;

    var found = cart.find(function (i) { return i.id === id; });
    if (found) {
        found.qty += 1;
    } else {
        cart.push({ id: id, title: title, price: price, img: img, qty: 1 });
    }

    saveCart();
    renderCart();
    openCart();
});

document.addEventListener("DOMContentLoaded", function () {
    var searchInput = document.getElementById("searchInput");
    var searchBtn = document.getElementById("searchBtn");
    var suggestions = document.getElementById("suggestions");
    var noResult = document.getElementById("noResult");
    var searchResults = document.getElementById("searchResults");

    var searchData = [
        
        { name: "Fabric & Textile", url: "/Products/FabricTextile", category: "Category" },
        { name: "Cushion Covers", url: "/Products/FabricCushionCovers", category: "Fabric & Textile" },
        { name: "Printed Kurtas", url: "/Products/FabricKurtas", category: "Fabric & Textile" },
        { name: "Tie-Dye Dupattas", url: "/Products/FabricDupattas", category: "Fabric & Textile" },
        { name: "Tote Bags", url: "/Products/FabricToteBags", category: "Fabric & Textile" },

       
        { name: "Wool & Yarn", url: "/Products/WoolYarn", category: "Category" },
        { name: "Sweaters", url: "/Products/WoolSweaters", category: "Wool & Yarn" },
        { name: "Crochet Bags", url: "/Products/WoolCrochetBags", category: "Wool & Yarn" },
        { name: "Storage Baskets", url: "/Products/WoolStorageBaskets", category: "Wool & Yarn" },
        { name: "Beanies", url: "/Products/WoolBeanies", category: "Wool & Yarn" },

       
        { name: "Leather", url: "/Products/Leather", category: "Category" },
        { name: "Leather Handbags", url: "/Products/LeatherHandbags", category: "Leather" },
        { name: "Wallets", url: "/Products/LeatherWallets", category: "Leather" },
        { name: "Belts", url: "/Products/LeatherBelts", category: "Leather" },
        { name: "Leather Keychains", url: "/Products/LeatherKeychains", category: "Leather" },
        { name: "Journals", url: "/Products/LeatherJournals", category: "Leather" },

       
        { name: "Wood", url: "/Products/Wood", category: "Category" },
        { name: "Photo Frames", url: "/Products/WoodPhotoFrames", category: "Wood" },
        { name: "Wood Necklaces", url: "/Products/WoodNecklaces", category: "Wood" },
        { name: "Wood Toys", url: "/Products/WoodToys", category: "Wood" },
        { name: "Serving Trays", url: "/Products/WoodServingTrays", category: "Wood" },
        { name: "Wood Showpieces", url: "/Products/WoodShowpieces", category: "Wood" },
        { name: "Wood Keychains", url: "/Products/WoodKeychains", category: "Wood" },

      
        { name: "Clay & Pottery", url: "/Products/ClayPottery", category: "Category" },
        { name: "Flower Pots", url: "/Products/ClayFlowerPots", category: "Clay & Pottery" },
        { name: "Coffee Mugs", url: "/Products/ClayCoffeeMugs", category: "Clay & Pottery" },
        { name: "Clay Vases", url: "/Products/ClayVases", category: "Clay & Pottery" },
        { name: "Clay Earrings", url: "/Products/ClayEarrings", category: "Clay & Pottery" },
        { name: "Clay Figurines", url: "/Products/ClayFigurines", category: "Clay & Pottery" },

        
        { name: "Jewels", url: "/Products/Jewels", category: "Category" },
        { name: "Necklaces", url: "/Products/JewelsNecklaces", category: "Jewels" },
        { name: "Bracelets", url: "/Products/JewelsBracelets", category: "Jewels" },
        { name: "Rings", url: "/Products/JewelsRings", category: "Jewels" },
        { name: "Bangles", url: "/Products/JewelsBangles", category: "Jewels" },
        { name: "Earrings", url: "/Products/JewelsEarrings", category: "Jewels" },

        
        { name: "Paper & Cards", url: "/Products/PaperCards", category: "Category" },
        { name: "Greeting Cards", url: "/Products/PaperGreetingCards", category: "Paper & Cards" },
        { name: "Gift Boxes", url: "/Products/PaperGiftBoxes", category: "Paper & Cards" },
        { name: "Paper Flowers", url: "/Products/PaperFlowers", category: "Paper & Cards" },
        { name: "Paper Showpieces", url: "/Products/PaperShowpieces", category: "Paper & Cards" },
        { name: "Scrapbooks", url: "/Products/PaperScrapbooks", category: "Paper & Cards" },

       
        { name: "Jute & Natural", url: "/Products/JuteNatural", category: "Category" },
        { name: "Jute Bags", url: "/Products/JuteBags", category: "Jute & Natural" },
        { name: "Baskets", url: "/Products/JuteBaskets", category: "Jute & Natural" },
        { name: "Trays", url: "/Products/JuteTrays", category: "Jute & Natural" },
        { name: "Dried Bouquets", url: "/Products/JuteDriedBouquets", category: "Jute & Natural" },
        { name: "Paintings", url: "/Products/JutePaintings", category: "Jute & Natural" },

        
        { name: "Resin & Epoxy", url: "/Products/ResinEpoxy", category: "Category" },
        { name: "Coasters", url: "/Products/ResinCoasters", category: "Resin & Epoxy" },
        { name: "Resin Earrings", url: "/Products/ResinEarrings", category: "Resin & Epoxy" },
        { name: "Resin Keychains", url: "/Products/ResinKeychains", category: "Resin & Epoxy" },
        { name: "Wall Art", url: "/Products/ResinWallArt", category: "Resin & Epoxy" },
        { name: "Resin Serving Trays", url: "/Products/ResinServingTrays", category: "Resin & Epoxy" },

        
        { name: "Metal & Wire", url: "/Products/MetalWire", category: "Category" },
        { name: "Metal Earrings", url: "/Products/MetalEarrings", category: "Metal & Wire" },
        { name: "Vases", url: "/Products/MetalVases", category: "Metal & Wire" },
        { name: "Metal Figurines", url: "/Products/MetalFigurines", category: "Metal & Wire" },
        { name: "Metal Wall Art", url: "/Products/MetalWallArt", category: "Metal & Wire" },
        { name: "Candle Holders", url: "/Products/MetalCandleHolders", category: "Metal & Wire" },
    ];

    if (searchInput) {
        searchInput.addEventListener("input", function () {
            var query = this.value.toLowerCase().trim();
            suggestions.innerHTML = "";
            suggestions.style.display = "none";
            noResult.style.display = "none";
            if (searchResults) searchResults.style.display = "none";
            if (query.length < 2) return;

            var matches = searchData.filter(function (item) {
                return item.name.toLowerCase().includes(query);
            });

            if (matches.length > 0) {
                matches.slice(0, 6).forEach(function (match) {
                    var li = document.createElement("li");
                    li.innerHTML = '<strong>' + match.name + '</strong> <span style="color:#999;font-size:0.85rem;">in ' + match.category + '</span>';
                    li.style.padding = "12px 15px";
                    li.style.cursor = "pointer";
                    li.style.borderBottom = "1px solid #f0f0f0";
                    li.addEventListener("click", function () {
                        window.location.href = match.url;
                    });
                    suggestions.appendChild(li);
                });
                suggestions.style.display = "block";
            } else {
               
                var li = document.createElement("li");
                li.innerHTML = '<i class="fa-solid fa-magnifying-glass" style="margin-right:8px;color:#a64d79;"></i> Search "<strong>' + query + '</strong>" in all products';
                li.style.padding = "12px 15px";
                li.style.cursor = "pointer";
                li.style.color = "#a64d79";
                li.addEventListener("click", function () {
                    window.location.href = "/Products/Search?query=" + encodeURIComponent(query);
                });
                suggestions.appendChild(li);
                suggestions.style.display = "block";
            }
        });

        if (searchBtn) searchBtn.addEventListener("click", performSearch);
        searchInput.addEventListener("keypress", function (e) {
            if (e.key === "Enter") performSearch();
        });

        function performSearch() {
            var query = searchInput.value.trim();
            if (query === "") return;
            suggestions.style.display = "none";

            var queryLower = query.toLowerCase();
            var matches = searchData.filter(function (item) {
                return item.name.toLowerCase().includes(queryLower);
            });

            if (matches.length === 1) {
               
                window.location.href = matches[0].url;
            } else if (matches.length > 1) {
               
                var resultsHtml = '<div style="margin-top:20px;"><h3 style="font-size:1.1rem;margin-bottom:15px;">Results:</h3>';
                matches.forEach(function (match) {
                    resultsHtml += '<a href="' + match.url + '" style="display:block;padding:12px 15px;margin-bottom:8px;background:#f9f9f9;border-radius:6px;text-decoration:none;color:#333;">' +
                        '<strong style="color:#a64d79;">' + match.name + '</strong><br>' +
                        '<small style="color:#999;">' + match.category + '</small></a>';
                });
                resultsHtml += '</div>';
                if (searchResults) {
                    searchResults.innerHTML = resultsHtml;
                    searchResults.style.display = "block";
                }
            } else {
               
                window.location.href = "/Products/Search?query=" + encodeURIComponent(query);
            }
        }
    }
});

document.addEventListener("DOMContentLoaded", function () {
    document.getElementById("searchIcon")?.addEventListener("click", function (e) {
        e.preventDefault();
        document.getElementById("searchOverlay")?.classList.add("active");
        document.getElementById("searchPopup")?.classList.add("active");
        document.body.style.overflow = "hidden";
    });

    document.getElementById("closeSearchPopup")?.addEventListener("click", function () {
        document.getElementById("searchOverlay")?.classList.remove("active");
        document.getElementById("searchPopup")?.classList.remove("active");
        document.body.style.overflow = "";
    });

    document.getElementById("searchOverlay")?.addEventListener("click", function () {
        document.getElementById("searchOverlay")?.classList.remove("active");
        document.getElementById("searchPopup")?.classList.remove("active");
        document.body.style.overflow = "";
    });
});


document.addEventListener("DOMContentLoaded", function () {
    var shopDropBtn = document.getElementById("shopDropBtn");
    var shopDropdownMenu = document.getElementById("shopDropdownMenu");
    var dropdown = shopDropBtn?.closest('.dropdown');

    if (shopDropBtn && shopDropdownMenu && dropdown) {
        shopDropBtn.addEventListener("click", function (e) {
            e.preventDefault();
            shopDropdownMenu.classList.toggle("show");
        });
        dropdown.addEventListener("mouseenter", function () {
            shopDropdownMenu.classList.add("show");
        });
        dropdown.addEventListener("mouseleave", function () {
            shopDropdownMenu.classList.remove("show");
        });
        document.addEventListener("click", function (e) {
            if (!dropdown.contains(e.target)) {
                shopDropdownMenu.classList.remove("show");
            }
        });
    }
});


document.addEventListener("DOMContentLoaded", function () {

    function showSidebar(overlayId, sidebarId) {
        document.getElementById(overlayId)?.classList.add("active");
        document.getElementById(sidebarId)?.classList.add("active");
        document.body.style.overflow = "hidden";
    }

    function hideSidebar(overlayId, sidebarId) {
        document.getElementById(overlayId)?.classList.remove("active");
        document.getElementById(sidebarId)?.classList.remove("active");
        document.body.style.overflow = "";
    }

    document.getElementById("loginIcon")?.addEventListener("click", function (e) {
        e.preventDefault();
        var returnInput = document.getElementById('returnUrlInput');
        if (returnInput) returnInput.value = window.location.pathname;
        showSidebar("loginOverlay", "loginPopup");
    });

    document.getElementById("closeLoginPopup")?.addEventListener("click", function () {
        hideSidebar("loginOverlay", "loginPopup");
        window.location.href = window.location.pathname;
    });

    document.getElementById("createAccountLink")?.addEventListener("click", function (e) {
        e.preventDefault();
        var regReturnInput = document.getElementById('registerReturnUrl');
        if (regReturnInput) regReturnInput.value = window.location.pathname;
        hideSidebar("loginOverlay", "loginPopup");
        showSidebar("registerOverlay", "registerSidebar");
    });
    document.getElementById("backToLogin")?.addEventListener("click", function (e) {
        e.preventDefault();
        hideSidebar("registerOverlay", "registerSidebar");
        showSidebar("loginOverlay", "loginPopup");
    });

    document.getElementById("createpasLink")?.addEventListener("click", function (e) {
        e.preventDefault();
        hideSidebar("loginOverlay", "loginPopup");
        showSidebar("recoverOverlay", "recoverSidebar");
    });

    document.getElementById("recoverCancel")?.addEventListener("click", function (e) {
        e.preventDefault();
        hideSidebar("recoverOverlay", "recoverSidebar");
        showSidebar("loginOverlay", "loginPopup");
    });

    document.getElementById("closeRegister")?.addEventListener("click", function () {
        hideSidebar("registerOverlay", "registerSidebar");
    });

    document.getElementById("closeRecover")?.addEventListener("click", function () {
        hideSidebar("recoverOverlay", "recoverSidebar");
    });

    var loginError = document.querySelector('#loginPopup .login-error');
    if (loginError) {
        showSidebar("loginOverlay", "loginPopup");
    }

    
    document.getElementById("footerLoginLink")?.addEventListener("click", function (e) {
        e.preventDefault();
        showSidebar("loginOverlay", "loginPopup");
    });

    document.getElementById("footerRegisterLink")?.addEventListener("click", function (e) {
        e.preventDefault();
        var regReturnInput = document.getElementById('registerReturnUrl');
        if (regReturnInput) regReturnInput.value = window.location.pathname;
        showSidebar("registerOverlay", "registerSidebar");
    });

    document.getElementById("footerCartLink")?.addEventListener("click", function (e) {
        e.preventDefault();
        openCart();
    });

    
    document.getElementById("cartIcon")?.addEventListener("click", function (e) {
        e.preventDefault();
        openCart();
    });

    document.getElementById("closeCart")?.addEventListener("click", function () {
        closeCart();
    });

    document.getElementById("cartOverlay")?.addEventListener("click", function () {
        closeCart();
    });

    document.getElementById("viewCartBtn")?.addEventListener("click", function () {
        closeCart();
        window.location.href = "/Cart/ShoppingCart";
    });

    document.getElementById("checkoutBtn")?.addEventListener("click", function () {
        closeCart();
        window.location.href = "/Cart/Checkout";
    });

    
    renderCart();
});


document.addEventListener('DOMContentLoaded', function () {
    var trackModal = document.getElementById('trackingModal');
    var trackBtn = document.getElementById('trackOrderBtn');

    if (!trackBtn || !trackModal) return;

    trackBtn.addEventListener('click', function () {
        trackModal.classList.add('active');
        document.body.style.overflow = 'hidden';
    });

    document.getElementById('closeTracking')?.addEventListener('click', function () {
        trackModal.classList.remove('active');
        document.body.style.overflow = '';
    });

    trackModal.addEventListener('click', function (e) {
        if (e.target === trackModal) {
            trackModal.classList.remove('active');
            document.body.style.overflow = '';
        }
    });
});