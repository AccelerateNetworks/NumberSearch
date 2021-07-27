function ownKeys(object, enumerableOnly) { var keys = Object.keys(object); if (Object.getOwnPropertySymbols) { var symbols = Object.getOwnPropertySymbols(object); if (enumerableOnly) symbols = symbols.filter(function (sym) { return Object.getOwnPropertyDescriptor(object, sym).enumerable; }); keys.push.apply(keys, symbols); } return keys; }

function _objectSpread(target) { for (var i = 1; i < arguments.length; i++) { var source = arguments[i] != null ? arguments[i] : {}; if (i % 2) { ownKeys(Object(source), true).forEach(function (key) { _defineProperty(target, key, source[key]); }); } else if (Object.getOwnPropertyDescriptors) { Object.defineProperties(target, Object.getOwnPropertyDescriptors(source)); } else { ownKeys(Object(source)).forEach(function (key) { Object.defineProperty(target, key, Object.getOwnPropertyDescriptor(source, key)); }); } } return target; }

function _defineProperty(obj, key, value) { if (key in obj) { Object.defineProperty(obj, key, { value: value, enumerable: true, configurable: true, writable: true }); } else { obj[key] = value; } return obj; }

/**
 * Accelerate Networks
 * Theme core scripts
 *
 * @author Andrew Stiefel
 * @version 1.0.0
 */
(function () {
  'use strict';
  /**
   * Enable sticky behaviour of navigation bar on page scroll
  */

  var stickyNavbar = function () {
    var navbar = document.querySelector('.navbar-sticky');
    if (navbar == null) return;
    var navbarClass = navbar.classList,
        navbarH = navbar.offsetHeight,
        scrollOffset = 500;

    if (navbarClass.contains('navbar-floating') && navbarClass.contains('navbar-dark')) {
      window.addEventListener('scroll', function (e) {
        if (e.currentTarget.pageYOffset > scrollOffset) {
          navbar.classList.remove('navbar-dark');
          navbar.classList.add('navbar-light', 'navbar-stuck');
        } else {
          navbar.classList.remove('navbar-light', 'navbar-stuck');
          navbar.classList.add('navbar-dark');
        }
      });
    } else if (navbarClass.contains('navbar-floating') && navbarClass.contains('navbar-light')) {
      window.addEventListener('scroll', function (e) {
        if (e.currentTarget.pageYOffset > scrollOffset) {
          navbar.classList.add('navbar-stuck');
        } else {
          navbar.classList.remove('navbar-stuck');
        }
      });
    } else {
      window.addEventListener('scroll', function (e) {
        if (e.currentTarget.pageYOffset > scrollOffset) {
          document.body.style.paddingTop = navbarH + 'px';
          navbar.classList.add('navbar-stuck');
        } else {
          document.body.style.paddingTop = '';
          navbar.classList.remove('navbar-stuck');
        }
      });
    }
  }();
  /**
   * Form validation
  */


  var formValidation = function () {
    var selector = 'needs-validation';
    window.addEventListener('load', function () {
      // Fetch all the forms we want to apply custom Bootstrap validation styles to
      var forms = document.getElementsByClassName(selector); // Loop over them and prevent submission

      var validation = Array.prototype.filter.call(forms, function (form) {
        form.addEventListener('submit', function (e) {
          if (form.checkValidity() === false) {
            e.preventDefault();
            e.stopPropagation();
          }

          form.classList.add('was-validated');
        }, false);
      });
    }, false);
  }();
  /**
   * Input fields formatter
   * @requires https://github.com/nosir/cleave.js
  */


  var inputFormatter = function () {
    var input = document.querySelectorAll('[data-format]');
    if (input.length === 0) return;

    for (var i = 0; i < input.length; i++) {
      var inputFormat = input[i].dataset.format,
          blocks = input[i].dataset.blocks,
          delimiter = input[i].dataset.delimiter;
      blocks = blocks !== undefined ? blocks.split(' ').map(Number) : '';
      delimiter = delimiter !== undefined ? delimiter : ' ';

      switch (inputFormat) {
        case 'card':
          var card = new Cleave(input[i], {
            creditCard: true
          });
          break;

        case 'cvc':
          var cvc = new Cleave(input[i], {
            numeral: true,
            numeralIntegerScale: 3
          });
          break;

        case 'date':
          var date = new Cleave(input[i], {
            date: true,
            datePattern: ['m', 'y']
          });
          break;

        case 'date-long':
          var dateLong = new Cleave(input[i], {
            date: true,
            delimiter: '-',
            datePattern: ['Y', 'm', 'd']
          });
          break;

        case 'time':
          var time = new Cleave(input[i], {
            time: true,
            datePattern: ['h', 'm']
          });
          break;

        case 'custom':
          var custom = new Cleave(input[i], {
            delimiter: delimiter,
            blocks: blocks
          });
          break;

        default:
          console.error('Sorry, your format ' + inputFormat + ' is not available. You can add it to the theme object method - inputFormatter in src/js/theme.js or choose one from the list of available formats: card, cvc, date, date-long, time or custom.');
      }
    }
  }();
  /**
   * Anchor smooth scrolling
   * @requires https://github.com/cferdinandi/smooth-scroll/
  */


  var smoothScroll = function () {
    var selector = '[data-scroll]',
        fixedHeader = '[data-scroll-header]',
        scroll = new SmoothScroll(selector, {
      speed: 800,
      speedAsDuration: true,
      offset: 40,
      header: fixedHeader,
      updateURL: false
    });
  }();
  /**
   * Offcanvas toggler
  */


  var offcanvas = function () {
    var offcanvasTogglers = document.querySelectorAll('[data-bs-toggle="offcanvas"]'),
        offcanvasDismissers = document.querySelectorAll('[data-bs-dismiss="offcanvas"]'),
        offcanvas = document.querySelectorAll('.offcanvas'),
        docBody = document.body,
        fixedElements = document.querySelectorAll('[data-fixed-element]'),
        hasScrollbar = window.innerWidth > docBody.clientWidth; // Creating backdrop

    var backdrop = document.createElement('div');
    backdrop.classList.add('offcanvas-backdrop'); // Open offcanvas function

    var offcanvasOpen = function offcanvasOpen(offcanvasID, toggler) {
      var backdropContainer = document.querySelector(offcanvasID).parentNode;
      backdropContainer.appendChild(backdrop);
      setTimeout(function () {
        backdrop.classList.add('show');
      }, 20);
      document.querySelector(offcanvasID).setAttribute('data-offcanvas-show', true);

      if (hasScrollbar) {
        docBody.style.paddingRight = '15px';

        if (fixedElements.length) {
          for (var i = 0; i < fixedElements.length; i++) {
            fixedElements[i].classList.add('right-15');
          }
        }
      }

      docBody.classList.add('offcanvas-open');
    }; // Close offcanvas function


    var offcanvasClose = function offcanvasClose() {
      for (var i = 0; i < offcanvas.length; i++) {
        offcanvas[i].removeAttribute('data-offcanvas-show');
      }

      backdrop.classList.remove('show');
      setTimeout(function () {
        backdrop.parentNode.removeChild(backdrop);
      }, 250);

      if (hasScrollbar) {
        docBody.style.paddingRight = 0;

        if (fixedElements.length) {
          for (var _i = 0; _i < fixedElements.length; _i++) {
            fixedElements[_i].classList.remove('right-15');
          }
        }
      }

      docBody.classList.remove('offcanvas-open');
    }; // Open offcanvas event handler


    for (var i = 0; i < offcanvasTogglers.length; i++) {
      offcanvasTogglers[i].addEventListener('click', function (e) {
        e.preventDefault();
        offcanvasOpen(e.currentTarget.dataset.bsTarget, e.currentTarget);
      });
    } // Close offcanvas event handler


    for (var _i2 = 0; _i2 < offcanvasDismissers.length; _i2++) {
      offcanvasDismissers[_i2].addEventListener('click', function (e) {
        e.preventDefault();
        offcanvasClose();
      });
    } // Close offcanvas by clicking on backdrop


    document.addEventListener('click', function (e) {
      if (e.target.classList[0] === 'offcanvas-backdrop') {
        offcanvasClose();
      }
    });
  }();
  /**
   * Animate scroll to top button in/off view
  */


  var scrollTopButton = function () {
    var element = document.querySelector('.btn-scroll-top'),
        scrollOffset = 600;
    if (element == null) return;
    var offsetFromTop = parseInt(scrollOffset, 10);
    window.addEventListener('scroll', function (e) {
      if (e.currentTarget.pageYOffset > offsetFromTop) {
        element.classList.add('show');
      } else {
        element.classList.remove('show');
      }
    });
  }();
  /**
   * Popover
   * @requires https://getbootstrap.com
   * @requires https://popper.js.org/
  */


  var popover = function () {
    var popoverTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="popover"]'));
    var popoverList = popoverTriggerList.map(function (popoverTriggerEl) {
      return new bootstrap.Popover(popoverTriggerEl);
    });
  }();
  /**
   * Lightbox component for presenting various types of media
   * @requires https://github.com/sachinchoolur/lightgallery.js
  */


  var gallery = function () {
    var gallery = document.querySelectorAll('.gallery');

    if (gallery.length) {
      for (var i = 0; i < gallery.length; i++) {
        lightGallery(gallery[i], {
          selector: '.gallery-item',
          download: false,
          videojs: true,
          youtubePlayerParams: {
            modestbranding: 1,
            showinfo: 0,
            rel: 0
          },
          vimeoPlayerParams: {
            byline: 0,
            portrait: 0,
            color: '766df4'
          }
        });
      }
    }
  }();
  /**
   * Content carousel with extensive options to control behaviour and appearance
   * @requires https://github.com/ganlanyuan/tiny-slider
   */


  var carousel = function () {
    // forEach function
    var forEach = function forEach(array, callback, scope) {
      for (var i = 0; i < array.length; i++) {
        callback.call(scope, i, array[i]); // passes back stuff we need
      }
    }; // Carousel initialization


    var carousels = document.querySelectorAll('.tns-carousel-wrapper .tns-carousel-inner');
    forEach(carousels, function (index, value) {
      var defaults = {
        container: value,
        controlsText: ['<i class="ai-arrow-left"></i>', '<i class="ai-arrow-right"></i>'],
        navPosition: 'top',
        controlsPosition: 'top',
        mouseDrag: true,
        speed: 600,
        autoplayHoverPause: true,
        autoplayButtonOutput: false
      };
      var userOptions;
      if (value.dataset.carouselOptions != undefined) userOptions = JSON.parse(value.dataset.carouselOptions);

      var options = _objectSpread(_objectSpread({}, defaults), userOptions);

      var carousel = tns(options);
      var carouselWrapper = value.closest('.tns-carousel-wrapper'),
          carouselItems = carouselWrapper.querySelectorAll('.tns-item'); // Custom pager

      var pager = carouselWrapper.querySelector('.tns-carousel-pager');

      if (pager != undefined) {
        var pageLinks = pager.querySelectorAll('[data-goto]');

        for (var i = 0; i < pageLinks.length; i++) {
          pageLinks[i].addEventListener('click', function (e) {
            carousel.goTo(this.dataset.goto - 1);
            e.preventDefault();
          });
        }

        carousel.events.on('indexChanged', function () {
          var info = carousel.getInfo();

          for (var n = 0; n < pageLinks.length; n++) {
            pageLinks[n].classList.remove('active');
          }

          pager.querySelector('[data-goto="' + info.displayIndex + '"]').classList.add('active');
        });
      } // Change label text


      var labelElem = carouselWrapper.querySelector('.tns-carousel-label');

      if (labelElem != undefined) {
        carousel.events.on('indexChanged', function () {
          var info = carousel.getInfo(),
              labelText = carouselItems[info.index].dataset.carouselLabel;
          labelElem.innerHTML = labelText;
        });
      } // Progress + slide counter


      if (carouselWrapper.querySelector('.tns-carousel-progress') === null) return;
      var carouselInfo = carousel.getInfo(),
          carouselCurrentSlide = carouselWrapper.querySelector('.tns-current-slide'),
          carouselTotalSlides = carouselWrapper.querySelector('.tns-total-slides'),
          carouselProgress = carouselWrapper.querySelector('.tns-carousel-progress .progress-bar');
      carouselCurrentSlide.innerHTML = carouselInfo.displayIndex;
      carouselTotalSlides.innerHTML = carouselInfo.slideCount;
      carouselProgress.style.width = carouselInfo.displayIndex / carouselInfo.slideCount * 100 + '%';
      carousel.events.on('indexChanged', function () {
        var info = carousel.getInfo();
        carouselCurrentSlide.innerHTML = info.displayIndex;
        carouselProgress.style.width = info.displayIndex / info.slideCount * 100 + '%';
      });
    });
  }();
  /**
   * Date / time picker
   * @requires https://github.com/flatpickr/flatpickr
   */


  var datePicker = function () {
    var picker = document.querySelectorAll('.date-picker');
    if (picker.length === 0) return;

    for (var i = 0; i < picker.length; i++) {
      var defaults = {
        disableMobile: 'true'
      };
      var userOptions = void 0;
      if (picker[i].dataset.datepickerOptions != undefined) userOptions = JSON.parse(picker[i].dataset.datepickerOptions);
      var linkedInput = picker[i].classList.contains('date-range') ? {
        "plugins": [new rangePlugin({
          input: picker[i].dataset.linkedInput
        })]
      } : '{}';

      var options = _objectSpread(_objectSpread(_objectSpread({}, defaults), linkedInput), userOptions);

      flatpickr(picker[i], options);
    }
  }();
  /**
   * Open YouTube / Vimeo video in lightbox
   * @requires https://github.com/sachinchoolur/lightgallery.js
  */


  var videoBtn = function () {
    var button = document.querySelectorAll('.btn-video');

    if (button.length) {
      for (var i = 0; i < button.length; i++) {
        lightGallery(button[i], {
          selector: 'this',
          download: false,
          videojs: true,
          youtubePlayerParams: {
            modestbranding: 1,
            showinfo: 0,
            rel: 0
          },
          vimeoPlayerParams: {
            byline: 0,
            portrait: 0,
            color: '766df4'
          }
        });
      }
    }
  }(); // import masonryGrid from './components/masonry-grid';
  // import rangeSlider from './components/range-slider';
  // import viewSwitcher from './components/view-switcher';
  // import checkboxToggleClass from './components/checkbox-toggle-class';
  // import masterCheckbox from './components/master-checkbox';

})();