/* ========================================
   ELClass Color Utilities
   JavaScript helpers for accessing CSS color variables
   ======================================== */

const ColorUtils = {
    /**
     * Get a CSS color variable value from the document root
     * @param {string} variableName - The CSS variable name (e.g., '--color-primary')
     * @returns {string} The color value
     */
    getCSSVariable(variableName) {
        return getComputedStyle(document.documentElement)
            .getPropertyValue(variableName)
            .trim();
    },

    /**
     * Get the primary color (#0053B3 - Deep Blue)
     * @returns {string} Primary color hex value
     */
    getPrimaryColor() {
        return this.getCSSVariable('--color-primary');
    },

    /**
     * Get the primary hover color
     * @returns {string} Primary hover color hex value
     */
    getPrimaryHoverColor() {
        return this.getCSSVariable('--color-primary-hover');
    },

    /**
     * Get the secondary color (#82DAFF - Light Blue)
     * @returns {string} Secondary color hex value
     */
    getSecondaryColor() {
        return this.getCSSVariable('--color-secondary');
    },

    /**
     * Get the accent color (#FF716B - Coral)
     * @returns {string} Accent color hex value
     */
    getAccentColor() {
        return this.getCSSVariable('--color-accent');
    },

    /**
     * Get the warning color (#FCC247 - Yellow)
     * @returns {string} Warning color hex value
     */
    getWarningColor() {
        return this.getCSSVariable('--color-warning');
    },

    /**
     * Get the success color
     * @returns {string} Success color hex value
     */
    getSuccessColor() {
        return this.getCSSVariable('--color-success');
    },

    /**
     * Get the danger color
     * @returns {string} Danger color hex value
     */
    getDangerColor() {
        return this.getCSSVariable('--color-danger');
    },

    /**
     * Get default SweetAlert2 configuration with ELClass colors
     * @returns {object} SweetAlert2 config object
     */
    getSwalDefaults() {
        return {
            confirmButtonColor: this.getPrimaryColor(),
            cancelButtonColor: '#6c757d',
            focusConfirm: false,
            customClass: {
                confirmButton: 'btn btn-primary mx-2',
                cancelButton: 'btn btn-secondary mx-2'
            },
            buttonsStyling: false
        };
    },

    /**
     * Show a SweetAlert with ELClass styling
     * @param {object} options - SweetAlert2 options
     * @returns {Promise} SweetAlert2 promise
     */
    showAlert(options) {
        return Swal.fire({
            ...this.getSwalDefaults(),
            ...options
        });
    },

    /**
     * Show a confirmation dialog with ELClass styling
     * @param {string} title - Alert title
     * @param {string} text - Alert text
     * @param {string} confirmText - Confirm button text
     * @param {string} cancelText - Cancel button text
     * @returns {Promise} SweetAlert2 promise
     */
    showConfirm(title, text, confirmText = 'Yes', cancelText = 'Cancel') {
        return this.showAlert({
            title: title,
            text: text,
            icon: 'warning',
            showCancelButton: true,
            confirmButtonText: confirmText,
            cancelButtonText: cancelText
        });
    },

    /**
     * Show a success alert with ELClass styling
     * @param {string} title - Alert title
     * @param {string} text - Alert text
     * @returns {Promise} SweetAlert2 promise
     */
    showSuccess(title, text) {
        return this.showAlert({
            title: title,
            text: text,
            icon: 'success',
            confirmButtonText: 'OK'
        });
    },

    /**
     * Show an error alert with ELClass styling
     * @param {string} title - Alert title
     * @param {string} text - Alert text
     * @returns {Promise} SweetAlert2 promise
     */
    showError(title, text) {
        return this.showAlert({
            title: title,
            text: text,
            icon: 'error',
            confirmButtonText: 'OK'
        });
    },

    /**
     * Apply a color to chart.js dataset
     * @param {string} colorVariable - CSS variable name (e.g., '--color-primary')
     * @param {number} alpha - Opacity value (0-1)
     * @returns {string} RGBA color string
     */
    getChartColor(colorVariable, alpha = 1) {
        const color = this.getCSSVariable(colorVariable);
        // Convert hex to RGB
        const r = parseInt(color.slice(1, 3), 16);
        const g = parseInt(color.slice(3, 5), 16);
        const b = parseInt(color.slice(5, 7), 16);
        return `rgba(${r}, ${g}, ${b}, ${alpha})`;
    },

    /**
     * Get a color palette for charts using ELClass colors
     * @returns {Array<string>} Array of color hex values
     */
    getChartPalette() {
        return [
            this.getPrimaryColor(),
            this.getSecondaryColor(),
            this.getAccentColor(),
            this.getSuccessColor(),
            this.getWarningColor(),
            this.getDangerColor()
        ];
    }
};

// Export for use with module systems if needed
if (typeof module !== 'undefined' && module.exports) {
    module.exports = ColorUtils;
}
