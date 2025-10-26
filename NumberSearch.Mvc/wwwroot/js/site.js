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
    let fileName = e.target.files[0].name;
    $('.custom-file-label').html(fileName);
});

let cartCounter = 0;

function AddToCart(type, id, quantity, element) {
    // Default to 1 unit if the "Add to Cart" button is pressed.
    const quantityDisplay = document.getElementById(id);
    if (quantityDisplay == null || quantityDisplay.value == null || quantityDisplay.value.length == 0) {
        quantity = 1;
        if (quantityDisplay != null) {
            quantityDisplay.value = quantity;
        }
    } else {
        quantity = quantityDisplay.value;
    }
    if (quantityDisplay != null) {
        quantityDisplay.disabled = true;
    }
    id = `${id}`.trim();
    let removeButton = `<button type="button" onclick="RemoveFromCart('${type}', '${id}', ${quantity}, this)" class="btn btn-outline-danger"><span class="d-none spinner-border spinner-border-sm mr-2" role="status"></span>Remove</button>`;
    let cart = $('#headerCart');
    let cartButton = $('#cartButton');
    let contactButton = $('#contactButton');
    let spinner = $(element).find('span');
    spinner.removeClass('d-none');
    let route = `/Cart/Add/${type}/${id}/${quantity}`;
    fetch(route)
        .then(response => {
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
                console.log(`Failed to add ${type} ${id} to cart.`)
                alert(`Failed to add ${type} ${id} to cart.`);
                spinner.addClass('d-none')
            }
            return response.text(); // or .text(), .blob(), etc.
        })
        .then(data => {
            console.log(data);
            if (data == id) {
                console.log(`Added ${type} ${id} to cart.`)
                spinner.addClass('d-none');
                $(element).replaceWith(removeButton);
                cartCounter = parseInt($('#cartCounter').text());
                cartCounter = cartCounter + parseInt(quantity);
                $(contactButton).addClass('d-none');
                $(cartButton).removeClass('d-none');
                $('#cartCounter').text(cartCounter).removeClass('d-none');
                if (type == 'Coupon') {
                    location.reload();
                }
            } else {
                console.log(`Failed to add ${type} ${id} to cart.`)
                alert(`Failed to add ${type} ${id} to cart.`);
                spinner.addClass('d-none')
            }
        })
        .catch(error => {
            console.error('There was a problem with the fetch operation:', error);
            console.log(`Failed to add ${type} ${id} to cart.`)
            alert(`Failed to add ${type} ${id} to cart.`);
            spinner.addClass('d-none')
        });
}

function RemoveFromCart(type, id, quantity, element) {
    const quantityDisplay = document.getElementById(id);
    if (quantityDisplay != null) {
        quantityDisplay.value = null;
        quantityDisplay.disabled = false;
    }
    let addButton = `<button type="submit" onclick="AddToCart('${type}', '${id}', null, this)" class="btn btn-outline-primary"><span class="d-none spinner-border spinner-border-sm mr-2" role="status"></span>Add to Cart</button>`;
    let spinner = $(element).find('span');
    spinner.removeClass('d-none');
    let route = `/Cart/Remove/${type}/${id}/${quantity}`;
    fetch(route)
        .then(response => {
            if (!response.ok) {
                throw new Error(`HTTP error! status: ${response.status}`);
                console.log(`Failed to add ${type} ${id} to cart.`)
                alert(`Failed to add ${type} ${id} to cart.`);
                spinner.addClass('d-none')
            }
            return response.text(); // or .text(), .blob(), etc.
        })
        .then(data => {
            if (data == id) {
                console.log(`Removed ${type} ${id} from cart.`)
                spinner.addClass('d-none');
                $(element).replaceWith(addButton);
                cartCounter = parseInt($('#cartCounter').text());
                cartCounter = cartCounter - parseInt(quantity);
                $('#cartCounter').text(cartCounter).removeClass('d-none');
            } else {
                console.log(`Failed to remove ${type} ${id} from cart.`)
                spinner.addClass('d-none')
            }
        }).catch(error => {
            console.error('There was a problem with the fetch operation:', error);
            console.log(`Failed to remove ${type} ${id} from cart.`)
            spinner.addClass('d-none')
        });
}

window.addEventListener('scroll', moveScrollIndicator);

function moveScrollIndicator() {
    let winScroll = document.body.scrollTop || document.documentElement.scrollTop;
    let height = document.documentElement.scrollHeight - document.documentElement.clientHeight;
    let scrolled = (winScroll / height) * 100;
    document.getElementById("scrollIndicator").style.width = scrolled + "%";
}

function AddExtensionRegistration(newClientId, ext, nameOrLocation, email, model, callerId, element) {
    let request = new XMLHttpRequest();
    let route = `/Add/NewClient/${newClientId}/ExtensionRegistration`;
    request.open('POST', route, true);
    request.setRequestHeader("Content-Type", "application/json;charset=UTF-8");
    request.onload = function () {
        if (this.response != null) {
            console.log(`Added ${ext} to the NewClient.`)
            let table = document.getElementById('regextstable');
            let row = table.insertRow(0);
            let extCell = row.insertCell(0);
            extCell.innerHTML = ext;
            let nameCell = row.insertCell(1);
            nameCell.innerHTML = nameOrLocation;
            let emailCell = row.insertCell(2);
            emailCell.innerHTML = email;
            let modelCell = row.insertCell(3);
            modelCell.innerHTML = model;
            let callerIdCell = row.insertCell(4);
            callerIdCell.innerHTML = callerId;
            let removeButton = `<button type='button' onclick='RemoveExtensionRegistration("${newClientId}", ${this.response}, this)' class="btn btn-outline-danger"> <span class="d-none spinner-border spinner-border-sm mr-2" role="status">&nbsp;</span>Remove</button>`;
            let removeCell = row.insertCell(5);
            removeCell.innerHTML = removeButton;
        } else {
            console.log(`Failed to add ${ext} to cart.`)
        }
    };
    request.send(JSON.stringify({
        "extensionRegistrationId": `${newClientId}`,
        "newClientId": `${newClientId}`,
        "extensionNumber": `${ext}`,
        "nameOrLocation": `${nameOrLocation}`,
        "email": `${email}`,
        "modelOfPhone": `${model}`,
        "outboundCallerId": `${callerId}`,
        "dateUpdated": "2021-09-19T08:10:00.521Z"
    }));
}

function RemoveExtensionRegistration(newClientId, extRegId, element) {
    let request = new XMLHttpRequest();
    let route = `/Remove/NewClient/${newClientId}/ExtensionRegistration/${extRegId}`;
    request.open('GET', route, true);
    request.onload = function () {
        if (this.response != null) {
            console.log(`Removed ${extRegId} from NewClient ${newClientId}.`)
            let row = element.parentNode.parentNode;
            row.parentNode.removeChild(row);
        } else {
            console.log(`Failed to remove ${extRegId} from NewClient ${newClientId}.`)
        }
    };
    request.send();
}

function AddNumberDescription(newClientId, phoneNumber, description, prefix, element) {
    let request = new XMLHttpRequest();
    let route = `/Add/NewClient/${newClientId}/NumberDescription`;
    request.open('POST', route, true);
    request.setRequestHeader("Content-Type", "application/json;charset=UTF-8");
    request.onload = function () {
        if (this.response != null) {
            console.log(`Added ${phoneNumber} to the NewClient.`)
            let table = document.getElementById('numdestable');
            let row = table.insertRow(0);
            let phoneNumberCell = row.insertCell(0);
            phoneNumberCell.innerHTML = phoneNumber;
            let descriptionCell = row.insertCell(1);
            descriptionCell.innerHTML = description;
            let prefixCell = row.insertCell(2);
            prefixCell.innerHTML = prefix;
            let removeButton = `<button type='button' onclick='RemoveNumberDescription("${newClientId}", ${this.response}, this)' class="btn btn-outline-danger"> <span class="d-none spinner-border spinner-border-sm mr-2" role="status">&nbsp;</span>Remove</button>`;
            let removeCell = row.insertCell(3);
            removeCell.innerHTML = removeButton;
        } else {
            console.log(`Failed to add ${phoneNumber} to cart.`)
        }
    };
    request.send(JSON.stringify({
        "numberDescriptionId": `${newClientId}`,
        "newClientId": `${newClientId}`,
        "phoneNumber": `${phoneNumber}`,
        "description": `${description}`,
        "prefix": `${prefix}`,
        "dateUpdated": "2021-09-21T01:23:45.125Z"
    }));
}

function RemoveNumberDescription(newClientId, extRegId, element) {
    let request = new XMLHttpRequest();
    let route = `/Remove/NewClient/${newClientId}/NumberDescription/${extRegId}`;
    request.open('GET', route, true);
    request.onload = function () {
        if (this.response != null) {
            console.log(`Removed ${extRegId} from NewClient ${newClientId}.`)
            let row = element.parentNode.parentNode;
            row.parentNode.removeChild(row);
        } else {
            console.log(`Failed to remove ${extRegId} from NewClient ${newClientId}.`)
        }
    };
    request.send();
}

function AddPhoneMenuOption(newClientId, menuOption, destination, description, element) {
    let request = new XMLHttpRequest();
    let route = `/Add/NewClient/${newClientId}/PhoneMenuOption`;
    request.open('POST', route, true);
    request.setRequestHeader("Content-Type", "application/json;charset=UTF-8");
    request.onload = function () {
        if (this.response != null) {
            console.log(`Added ${phoneNumber} to the NewClient.`)
            let table = document.getElementById('menuopttable');
            let row = table.insertRow(0);
            let menuOptionCell = row.insertCell(0);
            menuOptionCell.innerHTML = menuOption;
            let destinationCell = row.insertCell(1);
            destinationCell.innerHTML = destination;
            let descriptionCell = row.insertCell(2);
            descriptionCell.innerHTML = description;
            let removeButton = `<button type='button' onclick='RemovePhoneMenuOption("${newClientId}", ${this.response}, this)' class="btn btn-outline-danger"> <span class="d-none spinner-border spinner-border-sm mr-2" role="status">&nbsp;</span>Remove</button>`;
            let removeCell = row.insertCell(3);
            removeCell.innerHTML = removeButton;
        } else {
            console.log(`Failed to add ${menuOption} to cart.`)
        }
    };
    request.send(JSON.stringify({
        "phoneMenuOptionId": `${newClientId}`,
        "newClientId": `${newClientId}`,
        "menuOption": `${menuOption}`,
        "destination": `${destination}`,
        "description": `${description}`,
        "dateUpdated": "2021-09-21T01:23:45.125Z"
    }));
}

function RemovePhoneMenuOption(newClientId, extRegId, element) {
    let request = new XMLHttpRequest();
    let route = `/Remove/NewClient/${newClientId}/PhoneMenuOption/${extRegId}`;
    request.open('GET', route, true);
    request.onload = function () {
        if (this.response != null) {
            console.log(`Removed ${extRegId} from NewClient ${newClientId}.`)
            let row = element.parentNode.parentNode;
            row.parentNode.removeChild(row);
        } else {
            console.log(`Failed to remove ${extRegId} from NewClient ${newClientId}.`)
        }
    };
    request.send();
}

function AddIntercomRegistration(newClientId, outgoing, incoming, element) {
    let request = new XMLHttpRequest();
    let route = `/Add/NewClient/${newClientId}/IntercomRegistration`;
    request.open('POST', route, true);
    request.setRequestHeader("Content-Type", "application/json;charset=UTF-8");
    request.onload = function () {
        if (this.response != null) {
            console.log(`Added ${phoneNumber} to the NewClient.`)
            let table = document.getElementById('intercomtable');
            let row = table.insertRow(0);
            let outgoingCell = row.insertCell(0);
            outgoingCell.innerHTML = outgoing;
            let incomingCell = row.insertCell(1);
            incomingCell.innerHTML = incoming;
            let removeButton = `<button type='button' onclick='RemoveIntercomRegistration("${newClientId}", ${this.response}, this)' class="btn btn-outline-danger"> <span class="d-none spinner-border spinner-border-sm mr-2" role="status">&nbsp;</span>Remove</button>`;
            let removeCell = row.insertCell(2);
            removeCell.innerHTML = removeButton;
        } else {
            console.log(`Failed to add ${phoneNumber} to cart.`)
        }
    };
    request.send(JSON.stringify({
        "intercomRegistrationId": `${newClientId}`,
        "newClientId": `${newClientId}`,
        "extensionSendingIntercom": `${outgoing}`,
        "extensionRecievingIntercom": `${incoming}`,
        "dateUpdated": "2021-09-21T01:42:35.743Z"
    }));
}

function RemoveIntercomRegistration(newClientId, extRegId, element) {
    let request = new XMLHttpRequest();
    let route = `/Remove/NewClient/${newClientId}/IntercomRegistration/${extRegId}`;
    request.open('GET', route, true);
    request.onload = function () {
        if (this.response != null) {
            console.log(`Removed ${extRegId} from NewClient ${newClientId}.`)
            let row = element.parentNode.parentNode;
            row.parentNode.removeChild(row);
        } else {
            console.log(`Failed to remove ${extRegId} from NewClient ${newClientId}.`)
        }
    };
    request.send();
}

function AddSpeedDialKey(newClientId, numberOrExt, labelOrName, element) {
    let request = new XMLHttpRequest();
    let route = `/Add/NewClient/${newClientId}/SpeedDialKey`;
    request.open('POST', route, true);
    request.setRequestHeader("Content-Type", "application/json;charset=UTF-8");
    request.onload = function () {
        if (this.response != null) {
            console.log(`Added ${phoneNumber} to the NewClient.`)
            let table = document.getElementById('speeddialtable');
            let row = table.insertRow(0);
            let numberOrExtCell = row.insertCell(0);
            numberOrExtCell.innerHTML = numberOrExt;
            let incomingCell = row.insertCell(1);
            incomingCell.innerHTML = labelOrName;
            let removeButton = `<button type='button' onclick='RemoveSpeedDialKey("${newClientId}", ${this.response}, this)' class="btn btn-outline-danger"> <span class="d-none spinner-border spinner-border-sm mr-2" role="status">&nbsp;</span>Remove</button>`;
            let removeCell = row.insertCell(2);
            removeCell.innerHTML = removeButton;
        } else {
            console.log(`Failed to add ${phoneNumber} to cart.`)
        }
    };
    request.send(JSON.stringify({
        "speedDialKeyId": `${newClientId}`,
        "newClientId": `${newClientId}`,
        "numberOrExtension": `${numberOrExt}`,
        "labelOrName": `${labelOrName}`,
        "dateUpdated": "2021-09-21T02:57:39.917Z"
    }));
}

function RemoveSpeedDialKey(newClientId, extRegId, element) {
    let request = new XMLHttpRequest();
    let route = `/Remove/NewClient/${newClientId}/SpeedDialKey/${extRegId}`;
    request.open('GET', route, true);
    request.onload = function () {
        if (this.response != null) {
            console.log(`Removed ${extRegId} from NewClient ${newClientId}.`)
            let row = element.parentNode.parentNode;
            row.parentNode.removeChild(row);
        } else {
            console.log(`Failed to remove ${extRegId} from NewClient ${newClientId}.`)
        }
    };
    request.send();
}

function AddFollowMeRegistration(newClientId, extNum, cellPhone, forwardTo, element) {
    let request = new XMLHttpRequest();
    let route = `/Add/NewClient/${newClientId}/FollowMeRegistration`;
    request.open('POST', route, true);
    request.setRequestHeader("Content-Type", "application/json;charset=UTF-8");
    request.onload = function () {
        if (this.response != null) {
            console.log(`Added ${phoneNumber} to the NewClient.`)
            let table = document.getElementById('followMeTable');
            let row = table.insertRow(0);
            let extNumCell = row.insertCell(0);
            extNumCell.innerHTML = extNum;
            let cellPhoneCell = row.insertCell(1);
            cellPhoneCell.innerHTML = cellPhone;
            let forwardToCell = row.insertCell(2);
            forwardToCell.innerHTML = forwardTo;
            let removeButton = `<button type='button' onclick='RemoveFollowMeRegistration("${newClientId}", ${this.response}, this)' class="btn btn-outline-danger"> <span class="d-none spinner-border spinner-border-sm mr-2" role="status">&nbsp;</span>Remove</button>`;
            let removeCell = row.insertCell(3);
            removeCell.innerHTML = removeButton;
        } else {
            console.log(`Failed to add ${phoneNumber} to cart.`)
        }
    };
    request.send(JSON.stringify({
        "followMeRegistrationId": `${newClientId}`,
        "newClientId": `${newClientId}`,
        "numberOrExtension": `${extNum}`,
        "cellPhoneNumber": `${cellPhone}`,
        "unreachablePhoneNumber": `${forwardTo}`,
        "dateUpdated": "2021-09-21T03:19:38.314Z"
    }));
}

function RemoveFollowMeRegistration(newClientId, extRegId, element) {
    let request = new XMLHttpRequest();
    let route = `/Remove/NewClient/${newClientId}/FollowMeRegistration/${extRegId}`;
    request.open('GET', route, true);
    request.onload = function () {
        if (this.response != null) {
            console.log(`Removed ${extRegId} from NewClient ${newClientId}.`)
            let row = element.parentNode.parentNode;
            row.parentNode.removeChild(row);
        } else {
            console.log(`Failed to remove ${extRegId} from NewClient ${newClientId}.`)
        }
    };
    request.send();
}