import { Component, computed, inject, OnInit, signal } from '@angular/core';
import { NgTemplateOutlet } from '@angular/common';
import { RouterLink } from '@angular/router';
import { forkJoin, finalize } from 'rxjs';
import { formatAddress } from '../../../shared/models/address-model';
import { GetVendorsResponse } from '../../vendors/data-access/models/get-vendors-response';
import { VendorApiService } from '../../vendors/data-access/vendor-api';
import { ContactApiService } from '../data-access/contact-api';
import { Contact } from '../data-access/models/contact';

/**
 * Two-column partner page: vendors on the left, contacts on the right.
 * Selecting a vendor filters the contacts to the ones attached to it; the search box
 * filters both lists by vendor name, contact name, phone number and e-mail address.
 */
@Component({
  selector: 'app-partner-list',
  imports: [RouterLink, NgTemplateOutlet],
  templateUrl: './partner-list.html',
})
export class PartnerList implements OnInit {
  private readonly vendorApi = inject(VendorApiService);
  private readonly contactApi = inject(ContactApiService);

  readonly vendors = signal<readonly GetVendorsResponse[]>([]);
  readonly contacts = signal<readonly Contact[]>([]);
  readonly isLoading = signal(true);
  readonly errorMessage = signal<string | null>(null);

  readonly selectedVendorId = signal<string | null>(null);
  readonly expandedContactId = signal<string | null>(null);
  readonly searchTerm = signal('');
  readonly formatAddress = formatAddress;

  private readonly vendorNames = computed(
    () => new Map(this.vendors().map(vendor => [vendor.id, vendor.name])),
  );

  readonly selectedVendor = computed(
    () => this.vendors().find(vendor => vendor.id === this.selectedVendorId()) ?? null,
  );

  private readonly searchedContacts = computed(() => {
    const term = normalize(this.searchTerm());

    return term === '' ? this.contacts() : this.contacts().filter(contact => this.matches(contact, term));
  });

  readonly visibleVendors = computed(() => {
    const term = normalize(this.searchTerm());

    if (term === '') {
      return this.vendors();
    }

    const vendorIdsWithMatchingContacts = new Set(this.searchedContacts().map(contact => contact.vendorId));

    return this.vendors().filter(
      vendor => normalize(vendor.name).includes(term) || vendorIdsWithMatchingContacts.has(vendor.id),
    );
  });

  readonly visibleContacts = computed(() => {
    const vendorId = this.selectedVendorId();

    return vendorId === null
      ? this.searchedContacts()
      : this.searchedContacts().filter(contact => contact.vendorId === vendorId);
  });

  ngOnInit(): void {
    this.load();
  }

  load(): void {
    this.isLoading.set(true);
    this.errorMessage.set(null);

    forkJoin({ vendors: this.vendorApi.getAll(), contacts: this.contactApi.getAll() })
      .pipe(finalize(() => this.isLoading.set(false)))
      .subscribe({
        next: ({ vendors, contacts }) => {
          this.vendors.set(vendors);
          this.contacts.set(contacts);
        },
        error: error => {
          console.error('Failed to load the partners.', error);
          this.errorMessage.set('A partnerek jelenleg nem érhetők el.');
        },
      });
  }

  toggleVendor(vendorId: string): void {
    this.selectedVendorId.update(current => (current === vendorId ? null : vendorId));
    this.expandedContactId.set(null);
  }

  toggleContact(contactId: string): void {
    this.expandedContactId.update(current => (current === contactId ? null : contactId));
  }

  search(value: string): void {
    this.searchTerm.set(value);
    this.expandedContactId.set(null);
  }

  vendorName(contact: Contact): string {
    return contact.vendorId ? (this.vendorNames().get(contact.vendorId) ?? '') : '';
  }

  private matches(contact: Contact, term: string): boolean {
    const digits = /^[\d\s+()-]+$/.test(term) ? term.replace(/\D/g, '') : '';
    const text = [
      `${contact.lastName} ${contact.firstName}`,
      `${contact.firstName} ${contact.lastName}`,
      contact.emailAddress,
      contact.phoneNumber,
      this.vendorName(contact),
    ];

    return text.some(value => normalize(value).includes(term)) ||
      (digits.length > 0 && contact.phoneNumber.replace(/\D/g, '').includes(digits));
  }
}

/** Lower-cases and strips accents so that "kovacs" also finds "Kovács". */
function normalize(value: string): string {
  return value.normalize('NFD').replace(/[\u0300-\u036f]/g, '').toLowerCase().trim();
}
