// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

$(document).ready();

function displayBusyIndicator(element) {
    let spinner = '<span class="spinner-border" role="status" aria-hidden="true"></span>';
    $(element).html(spinner);
}

$(window).on('beforeunload', function () {
    displayBusyIndicator();
});

$(document).on('submit', 'form', function () {
    displayBusyIndicator();
});

$('input[type="file"]').change(function (e) {
    var fileName = e.target.files[0].name;
    $('.custom-file-label').html(fileName);
});

var cartCounter = 0;

function AddToCart(type, id, quantity, element) {
    // Default to 1 unit if the "Add to Cart" button is pressed.
    if (quantity.length === 0) {
        quantity = 1;
    }
    var removeButton = `<button onclick="RemoveFromCart('${type}', '${id}', ${quantity}, this)" class="btn btn-outline-danger"><span class="d-none spinner-border spinner-border-sm mr-2" role="status"></span>Remove</button>`;
    var cart = $('#headerCart');
    var cartButton = $('#cartButton');
    var contactButton = $('#contactButton');
    let spinner = $(element).find('span');
    spinner.removeClass('d-none');
    var request = new XMLHttpRequest();
    var route = `/Cart/Add/${type}/${id}/${quantity}`;
    request.open('GET', route, true);
    request.onload = function () {
        if (this.response == id) {
            console.log(`Added ${type} ${id} to cart.`)
            spinner.addClass('d-none');
            $(element).replaceWith(removeButton);
            cartCounter = parseInt($('#cartCounter').text());
            cartCounter++;
            $(contactButton).addClass('d-none');
            $(cartButton).removeClass('d-none');
            $('#cartCounter').text(cartCounter).removeClass('d-none');
            if (type == 'Coupon') {
                location.reload();
            }
        } else {
            console.log(`Failed to add ${type} ${id} to cart.`)
            spinner.addClass('d-none')
        }
    };
    request.send();
}

function RemoveFromCart(type, id, quantity, element) {
    var addButton = `<button onclick="AddToCart('${type}', '${id}', ${quantity}, this)" class="btn btn-outline-primary"><span class="d-none spinner-border spinner-border-sm mr-2" role="status"></span>Add to Cart</button>`;
    let spinner = $(element).find('span');
    spinner.removeClass('d-none');
    var request = new XMLHttpRequest();
    var route = `/Cart/Remove/${type}/${id}/${quantity}`;
    request.open('GET', route, true);
    request.onload = function () {
        if (this.response == id) {
            console.log(`Removed ${type} ${id} from cart.`)
            spinner.addClass('d-none');
            $(element).replaceWith(addButton);
            cartCounter = parseInt($('#cartCounter').text());
            cartCounter--;
            $('#cartCounter').text(cartCounter).removeClass('d-none');
        } else {
            console.log(`Failed to remove ${type} ${id} from cart.`)
            spinner.addClass('d-none')
        }
    };
    request.send();
}

window.addEventListener('scroll', moveScrollIndicator);

function moveScrollIndicator() {
    var winScroll = document.body.scrollTop || document.documentElement.scrollTop;
    var height = document.documentElement.scrollHeight - document.documentElement.clientHeight;
    var scrolled = (winScroll / height) * 100;
    document.getElementById("scrollIndicator").style.width = scrolled + "%";
}