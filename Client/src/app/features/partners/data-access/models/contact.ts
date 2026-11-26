/** A standalone contact person; vendorId is null when not attached to any vendor. */
export interface Contact {
  readonly id: string;
  readonly firstName: string;
  readonly lastName: string;
  readonly jobTitle: string;
  readonly phoneNumber: string;
  readonly emailAddress: string;
  readonly vendorId: string | null;
}

/** Request body used both for creating and for updating a contact. */
export interface SaveContactRequest {
  readonly firstName: string;
  readonly lastName: string;
  readonly jobTitle: string;
  readonly phoneNumber: string;
  readonly emailAddress: string;
}
