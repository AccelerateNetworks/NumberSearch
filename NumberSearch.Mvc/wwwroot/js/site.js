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

function addToCart(dialedNumber, element) {
    var removeButton = `<button onclick="removeFromCart(${dialedNumber}, this)" class="btn btn-outline-danger"><span class="d-none spinner-border spinner-border-sm" role="status"></span>&nbsp;Remove</button>`;
    let spinner = $(element).find('span');
    spinner.removeClass('d-none');
    var request = new XMLHttpRequest();
    var route = `/api/add/${dialedNumber}`;
    request.open('GET', route, true);
    request.onload = function () {
        if (this.response == dialedNumber) {
            console.log(`Added ${dialedNumber} to cart.`)
            spinner.addClass('d-none');
            $(element).replaceWith(removeButton);
        } else {
            console.log(`Failed to add ${dialedNumber} to cart.`)
            spinner.addClass('d-none')
        }
    };
    request.send();
}

function removeFromCart(dialedNumber, element) {
    var addButton = `<button onclick="addToCart(${dialedNumber}, this)" class="btn btn-outline-primary"><span class="d-none spinner-border spinner-border-sm" role="status"></span>&nbsp;Add to Cart</button>`;
    let spinner = $(element).find('span');
    spinner.removeClass('d-none');
    var request = new XMLHttpRequest();
    var route = `/api/remove/${dialedNumber}`;
    request.open('GET', route, true);
    request.onload = function () {
        if (this.response == dialedNumber) {
            console.log(`Removed ${dialedNumber} from cart.`)
            spinner.addClass('d-none');
            $(element).replaceWith(addButton);
        } else {
            console.log(`Failed to remove ${dialedNumber} from cart.`)
            spinner.addClass('d-none')
        }
    };
    request.send();
}