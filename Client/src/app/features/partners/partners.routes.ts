import { Routes } from '@angular/router';

export const partnersRoutes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    loadComponent: () => import('./partner-list/partner-list').then(component => component.PartnerList),
  },
  {
    path: 'vendors/new',
    loadComponent: () => import('./vendor-form/vendor-form').then(component => component.VendorForm),
  },
  {
    path: 'vendors/:id/edit',
    loadComponent: () => import('./vendor-form/vendor-form').then(component => component.VendorForm),
  },
  {
    path: 'contacts/new',
    loadComponent: () => import('./contact-form/contact-form').then(component => component.ContactForm),
  },
  {
    path: 'contacts/:id/edit',
    loadComponent: () => import('./contact-form/contact-form').then(component => component.ContactForm),
  },
];
