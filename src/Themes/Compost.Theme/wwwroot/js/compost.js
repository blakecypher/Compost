/**
 * Compost Theme JavaScript
 * Handles theme switching, mobile navigation, and interactive features
 */

class CompostTheme {
    constructor() {
        this.init();
    }

    init() {
        this.initThemeToggle();
        this.initMobileNavigation();
        this.initScrollEffects();
        this.initTooltips();
        this.initAnimations();
        this.initNotifications();
        this.initAccessibility();
        this.initActiveNavigation();
    }

    /**
     * Theme Management
     */
    initThemeToggle() {
        const themeToggle = document.getElementById('theme-toggle');
        const themeIcon = document.getElementById('theme-icon');
        const html = document.documentElement;

        // Load saved theme or use system preference
        const savedTheme = localStorage.getItem('compost-theme') || 'auto';
        this.setTheme(savedTheme);

        themeToggle?.addEventListener('click', () => {
            const currentTheme = html.getAttribute('data-bs-theme') || html.getAttribute('data-theme') || 'auto';
            const newTheme = this.getNextTheme(currentTheme);
            this.setTheme(newTheme);
            localStorage.setItem('compost-theme', newTheme);
        });
    }

    getNextTheme(current) {
        const themes = ['auto', 'light', 'dark'];
        const currentIndex = themes.indexOf(current);
        return themes[(currentIndex + 1) % themes.length];
    }

    setTheme(theme) {
        const html = document.documentElement;
        const themeIcon = document.getElementById('theme-icon');

        html.setAttribute('data-bs-theme', theme);
        html.setAttribute('data-theme', theme);

        // Update icon
        if (themeIcon) {
            const icons = {
                'auto': 'fas fa-adjust',
                'light': 'fas fa-sun',
                'dark': 'fas fa-moon'
            };
            themeIcon.className = icons[theme] || icons['auto'];
        }

        // Update meta theme-color for mobile browsers
        const metaThemeColor = document.querySelector('meta[name="theme-color"]');
        if (metaThemeColor) {
            const colors = {
                'auto': '#2563eb',
                'light': '#2563eb',
                'dark': '#1e40af'
            };
            metaThemeColor.content = colors[theme] || colors['auto'];
        }
    }

    /**
     * Mobile Navigation
     */
    initMobileNavigation() {
        const mobileToggle = document.getElementById('mobile-menu-toggle');
        const mobileNav = document.getElementById('mobile-nav');

        mobileToggle?.addEventListener('click', () => {
            const isExpanded = mobileToggle.getAttribute('aria-expanded') === 'true';
            
            mobileToggle.setAttribute('aria-expanded', !isExpanded);
            mobileNav?.classList.toggle('show');
            
            // Update icon
            const icon = mobileToggle.querySelector('i');
            if (icon) {
                icon.className = isExpanded ? 'fas fa-bars' : 'fas fa-times';
            }
        });

        // Close mobile nav when clicking outside
        document.addEventListener('click', (e) => {
            if (!e.target.closest('.navbar') && mobileNav?.classList.contains('show')) {
                mobileNav.classList.remove('show');
                mobileToggle?.setAttribute('aria-expanded', 'false');
                const icon = mobileToggle?.querySelector('i');
                if (icon) icon.className = 'fas fa-bars';
            }
        });

        // Close mobile nav on window resize
        window.addEventListener('resize', () => {
            if (window.innerWidth >= 768 && mobileNav?.classList.contains('show')) {
                mobileNav.classList.remove('show');
                mobileToggle?.setAttribute('aria-expanded', 'false');
                const icon = mobileToggle?.querySelector('i');
                if (icon) icon.className = 'fas fa-bars';
            }
        });
    }

    /**
     * Scroll Effects
     */
    initScrollEffects() {
        const navbar = document.querySelector('.navbar');
        let lastScrollY = window.scrollY;

        window.addEventListener('scroll', () => {
            const currentScrollY = window.scrollY;
            
            // Add/remove scrolled class for styling
            if (currentScrollY > 50) {
                navbar?.classList.add('scrolled');
            } else {
                navbar?.classList.remove('scrolled');
            }

            // Hide/show navbar on scroll (desktop only)
            if (window.innerWidth >= 768) {
                if (currentScrollY > lastScrollY && currentScrollY > 100) {
                    // Scrolling down
                    navbar?.style.transform = 'translateY(-100%)';
                } else {
                    // Scrolling up
                    navbar?.style.transform = 'translateY(0)';
                }
            }

            lastScrollY = currentScrollY;
        });
    }

    /**
     * Tooltips
     */
    initTooltips() {
        // Initialize Bootstrap tooltips
        const tooltipTriggerList = [].slice.call(document.querySelectorAll('[data-bs-toggle="tooltip"]'));
        tooltipTriggerList.map(function (tooltipTriggerEl) {
            return new bootstrap.Tooltip(tooltipTriggerEl);
        });

        // Add custom tooltips for accessibility
        document.querySelectorAll('[title]').forEach(element => {
            if (!element.hasAttribute('data-bs-toggle')) {
                element.addEventListener('mouseenter', (e) => {
                    this.showCustomTooltip(e.target);
                });
                
                element.addEventListener('mouseleave', (e) => {
                    this.hideCustomTooltip(e.target);
                });
            }
        });
    }

    showCustomTooltip(element) {
        const title = element.getAttribute('title');
        if (!title) return;

        const tooltip = document.createElement('div');
        tooltip.className = 'custom-tooltip';
        tooltip.textContent = title;
        tooltip.style.cssText = `
            position: absolute;
            background: var(--bg-tertiary);
            color: var(--text-primary);
            padding: 0.5rem;
            border-radius: 6px;
            font-size: 0.875rem;
            z-index: 1000;
            pointer-events: none;
            opacity: 0;
            transition: opacity 0.2s;
        `;

        document.body.appendChild(tooltip);

        const rect = element.getBoundingClientRect();
        tooltip.style.left = rect.left + (rect.width / 2) - (tooltip.offsetWidth / 2) + 'px';
        tooltip.style.top = rect.top - tooltip.offsetHeight - 8 + 'px';

        // Fade in
        requestAnimationFrame(() => {
            tooltip.style.opacity = '1';
        });

        element._customTooltip = tooltip;
    }

    hideCustomTooltip(element) {
        if (element._customTooltip) {
            element._customTooltip.style.opacity = '0';
            setTimeout(() => {
                if (element._customTooltip && element._customTooltip.parentNode) {
                    element._customTooltip.parentNode.removeChild(element._customTooltip);
                }
                delete element._customTooltip;
            }, 200);
        }
    }

    /**
     * Animations
     */
    initAnimations() {
        // Intersection Observer for scroll animations
        const observerOptions = {
            threshold: 0.1,
            rootMargin: '0px 0px -50px 0px'
        };

        const observer = new IntersectionObserver((entries) => {
            entries.forEach(entry => {
                if (entry.isIntersecting) {
                    entry.target.classList.add('animate-fade-in');
                    observer.unobserve(entry.target);
                }
            });
        }, observerOptions);

        // Observe elements for animation
        document.querySelectorAll('.card, .glass-card').forEach(el => {
            observer.observe(el);
        });

        // Smooth scroll for anchor links
        document.querySelectorAll('a[href^="#"]').forEach(anchor => {
            anchor.addEventListener('click', (e) => {
                e.preventDefault();
                const target = document.querySelector(anchor.getAttribute('href'));
                if (target) {
                    target.scrollIntoView({
                        behavior: 'smooth',
                        block: 'start'
                    });
                }
            });
        });
    }

    /**
     * Notifications
     */
    initNotifications() {
        // Auto-hide alerts after 5 seconds
        document.querySelectorAll('.alert:not(.alert-permanent)').forEach(alert => {
            setTimeout(() => {
                if (alert.parentNode) {
                    alert.style.opacity = '0';
                    setTimeout(() => {
                        if (alert.parentNode) {
                            alert.parentNode.removeChild(alert);
                        }
                    }, 300);
                }
            }, 5000);
        });

        // Add close button to alerts that don't have one
        document.querySelectorAll('.alert:not(.alert-dismissible)').forEach(alert => {
            const closeBtn = document.createElement('button');
            closeBtn.type = 'button';
            closeBtn.className = 'btn-close';
            closeBtn.setAttribute('data-bs-dismiss', 'alert');
            closeBtn.setAttribute('aria-label', 'Close');
            alert.appendChild(closeBtn);
            alert.classList.add('alert-dismissible');
        });
    }

    /**
     * Accessibility
     */
    initAccessibility() {
        // Focus management for modals
        document.querySelectorAll('.modal').forEach(modal => {
            modal.addEventListener('shown.bs.modal', () => {
                const focusableElements = modal.querySelectorAll(
                    'button, [href], input, select, textarea, [tabindex]:not([tabindex="-1"])'
                );
                const firstFocusable = focusableElements[0];
                if (firstFocusable) {
                    firstFocusable.focus();
                }
            });
        });

        // Skip link functionality
        const skipLink = document.querySelector('a[href="#main-content"]');
        skipLink?.addEventListener('click', (e) => {
            e.preventDefault();
            const mainContent = document.getElementById('main-content');
            if (mainContent) {
                mainContent.setAttribute('tabindex', '-1');
                mainContent.focus();
                mainContent.removeAttribute('tabindex');
            }
        });

        // Keyboard navigation for dropdowns
        document.querySelectorAll('.dropdown').forEach(dropdown => {
            const toggle = dropdown.querySelector('.dropdown-toggle');
            const menu = dropdown.querySelector('.dropdown-menu');
            
            toggle?.addEventListener('keydown', (e) => {
                if (e.key === 'Enter' || e.key === ' ') {
                    e.preventDefault();
                    const isOpen = menu?.classList.contains('show');
                    if (isOpen) {
                        bootstrap.Dropdown.getInstance(toggle)?.hide();
                    } else {
                        bootstrap.Dropdown.getInstance(toggle)?.show();
                    }
                }
            });
        });

        // Add ARIA labels dynamically
        this.addAriaLabels();
    }

    addAriaLabels() {
        // Add aria-label to icons without text
        document.querySelectorAll('.btn i:only-child').forEach(icon => {
            const btn = icon.parentElement;
            if (!btn.getAttribute('aria-label') && !btn.textContent.trim()) {
                const iconClass = icon.className;
                let label = 'Button';
                
                if (iconClass.includes('fa-home')) label = 'Home';
                else if (iconClass.includes('fa-search')) label = 'Search';
                else if (iconClass.includes('fa-cog')) label = 'Settings';
                else if (iconClass.includes('fa-user')) label = 'User';
                else if (iconClass.includes('fa-bars')) label = 'Menu';
                else if (iconClass.includes('fa-times')) label = 'Close';
                
                btn.setAttribute('aria-label', label);
            }
        });
    }

    /**
     * Utility Methods
     */
    showNotification(message, type = 'info', duration = 5000) {
        const alert = document.createElement('div');
        alert.className = `alert alert-${type} alert-dismissible fade show position-fixed`;
        alert.style.cssText = 'top: 20px; right: 20px; z-index: 9999; min-width: 300px;';
        alert.innerHTML = `
            ${message}
            <button type="button" class="btn-close" data-bs-dismiss="alert" aria-label="Close"></button>
        `;

        document.body.appendChild(alert);

        if (duration > 0) {
            setTimeout(() => {
                if (alert.parentNode) {
                    alert.style.opacity = '0';
                    setTimeout(() => {
                        if (alert.parentNode) {
                            alert.parentNode.removeChild(alert);
                        }
                    }, 300);
                }
            }, duration);
        }

        return alert;
    }

    debounce(func, wait) {
        let timeout;
        return function executedFunction(...args) {
            const later = () => {
                clearTimeout(timeout);
                func(...args);
            };
            clearTimeout(timeout);
            timeout = setTimeout(later, wait);
        };
    }

    /**
     * Active Navigation State
     */
    initActiveNavigation() {
        const currentPath = window.location.pathname;
        const navLinks = document.querySelectorAll('.navbar-nav .nav-link');
        
        navLinks.forEach(link => {
            const href = link.getAttribute('href');
            if (href && this.isPathMatch(currentPath, href)) {
                link.classList.add('active');
            } else {
                link.classList.remove('active');
            }
        });
        
        // Also handle mobile navigation
        const mobileNavLinks = document.querySelectorAll('.mobile-nav .nav-link');
        mobileNavLinks.forEach(link => {
            const href = link.getAttribute('href');
            if (href && this.isPathMatch(currentPath, href)) {
                link.classList.add('active');
            } else {
                link.classList.remove('active');
            }
        });
    }
    
    isPathMatch(currentPath, href) {
        // Exact match for root
        if (href === '/' && currentPath === '/') {
            return true;
        }
        
        // Handle special cases
        if (href.includes('/Contexts/TreeView') && currentPath.includes('/Contexts/')) {
            return true;
        }
        
        // General path matching
        const hrefPath = href.replace('/', '');
        const currentPathClean = currentPath.replace('/', '');
        
        return hrefPath && currentPathClean.includes(hrefPath);
    }

    throttle(func, limit) {
        let inThrottle;
        return function() {
            const args = arguments;
            const context = this;
            if (!inThrottle) {
                func.apply(context, args);
                inThrottle = true;
                setTimeout(() => inThrottle = false, limit);
            }
        };
    }
}

// Initialize theme when DOM is ready
document.addEventListener('DOMContentLoaded', () => {
    window.compostTheme = new CompostTheme();
});

// Export for external use
window.CompostTheme = CompostTheme;
