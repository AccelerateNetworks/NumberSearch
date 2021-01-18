// Please see documentation at https://docs.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.
function displayBusyIndicator() {
    document.getElementById("loading").style.display = "block";
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

function PhoneNumberAdd(dialedNumber, element) {
    var removeButton = `<button onclick="PhoneNumberRemove(${dialedNumber}, this)" class="btn btn-outline-danger"><span class="d-none spinner-border spinner-border-sm mr-2" role="status"></span>Remove</button>`;
    var checkoutCart = `<a id="headerCart" class="btn btn-outline-success btn-lg" href="/Cart">Checkout&nbsp;<span id="cartCounter" class="badge badge-success badge-pill">0</span></a>`;
    var spinner = $(element).find('span');
    var cart = $('#headerCart');
    var counter = $('#cartCounter');
    spinner.removeClass('d-none');
    var request = new XMLHttpRequest();
    var route = `/Cart/PhoneNumber/Add/${dialedNumber}`;
    request.open('GET', route, true);
    request.onload = function () {
        if (this.response == dialedNumber) {
            console.log(`Added ${dialedNumber} to cart.`)
            spinner.addClass('d-none');
            $(element).replaceWith(removeButton);
            var count = parseInt(counter.text());
            $(cart).replaceWith(checkoutCart);
            counter = $('#cartCounter');
            counter.text(count + 1);;
            counter.removeClass('d-none');
        } else {
            console.log(`Failed to add ${dialedNumber} to cart.`)
            spinner.addClass('d-none')
        }
    };
    request.send();
}

function PhoneNumberRemove(dialedNumber, element) {
    var addButton = `<button onclick="PhoneNumberAdd(${dialedNumber}, this)" class="btn btn-outline-primary"><span class="d-none spinner-border spinner-border-sm mr-2" role="status"></span>Add to Cart</button>`;
    var spinner = $(element).find('span');
    var cart = $('#headerCart');
    var counter = $('#cartCounter');
    spinner.removeClass('d-none');
    var request = new XMLHttpRequest();
    var route = `/Cart/PhoneNumber/Remove/${dialedNumber}`;
    request.open('GET', route, true);
    request.onload = function () {
        if (this.response == dialedNumber) {
            console.log(`Removed ${dialedNumber} from cart.`)
            spinner.addClass('d-none');
            $(element).replaceWith(addButton);
            var count = parseInt($(counter).text());
            counter.text(count - 1);
            counter.removeClass('d-none');
        } else {
            console.log(`Failed to remove ${dialedNumber} from cart.`)
            spinner.addClass('d-none')
        }
    };
    request.send();
}

function ServiceAdd(serviceId, quantity, element) {
    var removeButton = `<button onclick="ServiceRemove(${serviceId}, this)" class="btn btn-outline-danger"><span class="d-none spinner-border spinner-border-sm mr-2" role="status"></span>Remove</button>`;
    let spinner = $(element).find('span');
    spinner.removeClass('d-none');
    var request = new XMLHttpRequest();
    var route = `/Cart/Service/Add/${dialedNumber}?quantity=${quantity}`;
    request.open('GET', route, true);
    request.onload = function () {
        if (this.response == dialedNumber) {
            console.log(`Added ${serviceId} to cart.`)
            spinner.addClass('d-none');
            $(element).replaceWith(removeButton);
        } else {
            console.log(`Failed to add ${serviceId} to cart.`)
            spinner.addClass('d-none')
        }
    };
    request.send();
}

function ServiceRemove(serviceId, element) {
    var addButton = `<button onclick="ServiceAdd(${serviceId}, this)" class="btn btn-outline-primary"><span class="d-none spinner-border spinner-border-sm mr-2" role="status"></span>Add to Cart</button>`;
    let spinner = $(element).find('span');
    spinner.removeClass('d-none');
    var request = new XMLHttpRequest();
    var route = `/Cart/Service/Remove/${serviceId}`;
    request.open('GET', route, true);
    request.onload = function () {
        if (this.response == dialedNumber) {
            console.log(`Removed ${serviceId} from cart.`)
            spinner.addClass('d-none');
            $(element).replaceWith(addButton);
        } else {
            console.log(`Failed to remove ${serviceId} from cart.`)
            spinner.addClass('d-none')
        }
    };
    request.send();
}