﻿// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// Главная функция инициализации
document.addEventListener('DOMContentLoaded', function () {
    // Инициализация боковой навигации
    initSidebar();
    
    // Анимации для карточек
    initCardAnimations();
    
    // Анимации для форм
    initFormAnimations();
    
    // Анимации для прогресс-баров
    initProgressBars();
});

// Функция инициализации боковой навигации
function initSidebar() {
    const sidebarToggle = document.getElementById('sidebarToggle');
    const sidebar = document.getElementById('sidebar');
    
    if (sidebarToggle && sidebar) {
        sidebarToggle.addEventListener('click', function() {
            sidebar.classList.toggle('active');
            
            // Добавляем класс для анимации значка
            this.classList.toggle('active');
        });
        
        // Закрытие сайдбара при клике вне его
        document.addEventListener('click', function(event) {
            if (sidebar.classList.contains('active') && 
                !sidebar.contains(event.target) && 
                !sidebarToggle.contains(event.target)) {
                sidebar.classList.remove('active');
                sidebarToggle.classList.remove('active');
            }
        });
        
        // Активный класс для текущей страницы
        const currentUrl = window.location.pathname;
        const navLinks = document.querySelectorAll('.sidebar-nav .nav-link');
        
        navLinks.forEach(link => {
            if (link.getAttribute('href') === currentUrl) {
                link.classList.add('active');
            }
        });
    }
}

// Функция для анимации карточек при скролле
function initCardAnimations() {
    const cards = document.querySelectorAll('.card');
    
    if (cards.length > 0) {
        // Настройка карточек
        cards.forEach((card, index) => {
            card.style.opacity = '0';
            card.style.transform = 'translateY(20px)';
            card.style.transition = 'opacity 0.5s ease, transform 0.5s ease';
            card.style.transitionDelay = (index * 0.1) + 's';
        });
        
        // Функция проверки видимости карточек
        function checkCardVisibility() {
            cards.forEach(card => {
                const rect = card.getBoundingClientRect();
                const isVisible = (rect.top <= window.innerHeight * 0.8);
                
                if (isVisible) {
                    card.style.opacity = '1';
                    card.style.transform = 'translateY(0)';
                }
            });
        }
        
        // Проверка при загрузке и скролле
        window.addEventListener('scroll', checkCardVisibility);
        window.addEventListener('resize', checkCardVisibility);
        checkCardVisibility();
    }
}

// Функция для анимации форм
function initFormAnimations() {
    const forms = document.querySelectorAll('form');
    
    forms.forEach(form => {
        const formElements = form.querySelectorAll('input, select, textarea, button');
        
        formElements.forEach((element, index) => {
            element.classList.add('fade-in');
            element.style.animationDelay = (index * 0.1) + 's';
        });
    });
}

// Функция для анимации прогресс-баров
function initProgressBars() {
    const progressBars = document.querySelectorAll('.progress-bar');
    
    function animateProgressBar() {
        progressBars.forEach(bar => {
            const width = bar.getAttribute('aria-valuenow') + '%';
            
            // Начинаем с 0 ширины
            bar.style.width = '0%';
            
            // Анимируем до целевой ширины
            setTimeout(() => {
                bar.style.width = width;
            }, 200);
        });
    }
    
    // Анимируем при загрузке страницы
    if (progressBars.length > 0) {
        animateProgressBar();
    }
}

// Открытие/закрытие модальных окон
function toggleModal(modalId) {
    const modal = document.getElementById(modalId);
    
    if (modal) {
        modal.classList.toggle('show');
        document.body.classList.toggle('modal-open');
    }
}

// Функция подтверждения действий с красивой анимацией
function confirmAction(message, callback) {
    const result = confirm(message);
    
    if (result && typeof callback === 'function') {
        callback();
    }
}
