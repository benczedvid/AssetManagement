/**
 * International phone number format, kept in sync with Contact.PhoneNumberPattern on the backend:
 * '+' and country code, then 6-12 digits optionally grouped by single spaces or hyphens.
 */
export const PHONE_NUMBER_PATTERN = /^\+[1-9][0-9]{0,2}(?:[ -]?[0-9]){6,12}$/;

export const PHONE_NUMBER_EXAMPLE = '+36 30 123 4567';
