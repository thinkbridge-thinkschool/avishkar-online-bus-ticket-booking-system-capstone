import { Routes } from '@angular/router';
import { authGuard } from './core/guards/auth.guard';
import { vendorGuard } from './core/guards/vendor.guard';
import { adminGuard } from './core/guards/admin.guard';
import { superAdminGuard } from './core/guards/super-admin.guard';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () => import('./features/home/home').then(m => m.HomeComponent),
  },
  {
    path: 'signup',
    loadComponent: () => import('./features/signup/signup').then(m => m.SignupComponent),
  },
  {
    path: 'search',
    loadComponent: () =>
      import('./features/search-results/search-results').then(m => m.SearchResultsComponent),
  },
  {
    path: 'schedules/:id',
    loadComponent: () =>
      import('./features/schedule-detail/schedule-detail').then(m => m.ScheduleDetailComponent),
  },
  {
    path: 'book/:scheduleId',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/booking/booking-new').then(m => m.BookingNewComponent),
  },
  {
    path: 'bookings',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/booking/my-bookings').then(m => m.MyBookingsComponent),
  },
  {
    path: 'bookings/:id',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/booking/booking-detail').then(m => m.BookingDetailComponent),
  },
  {
    path: 'payment/confirm',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/payment/payment-confirm').then(m => m.PaymentConfirmComponent),
  },
  {
    path: 'payment/:bookingId',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/payment/payment-process').then(m => m.PaymentProcessComponent),
  },
  {
    path: 'feedback/:bookingId',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/feedback/feedback-form').then(m => m.FeedbackFormComponent),
  },
  {
    path: 'profile',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/profile/profile').then(m => m.ProfileComponent),
  },
  {
    path: 'my-tenant',
    canActivate: [authGuard],
    loadComponent: () =>
      import('./features/vendor/vendor-tenant').then(m => m.VendorTenantComponent),
  },
  {
    path: 'vendor/dashboard',
    canActivate: [vendorGuard],
    loadComponent: () =>
      import('./features/vendor/vendor-dashboard').then(m => m.VendorDashboardComponent),
  },
  {
    path: 'vendor/buses',
    canActivate: [vendorGuard],
    loadComponent: () =>
      import('./features/vendor/vendor-buses').then(m => m.VendorBusesComponent),
  },
  {
    path: 'vendor/schedules',
    canActivate: [vendorGuard],
    loadComponent: () =>
      import('./features/vendor/vendor-schedules').then(m => m.VendorSchedulesComponent),
  },
  {
    path: 'admin/dashboard',
    canActivate: [superAdminGuard],
    loadComponent: () =>
      import('./features/admin/admin-dashboard').then(m => m.AdminDashboardComponent),
  },
  {
    path: 'admin/tenants',
    canActivate: [superAdminGuard],
    loadComponent: () =>
      import('./features/admin/admin-tenants').then(m => m.AdminTenantsComponent),
  },
  {
    path: 'admin/cities',
    canActivate: [adminGuard],
    loadComponent: () =>
      import('./features/admin/admin-cities').then(m => m.AdminCitiesComponent),
  },
  {
    path: 'admin/routes',
    canActivate: [adminGuard],
    loadComponent: () =>
      import('./features/admin/admin-routes').then(m => m.AdminRoutesComponent),
  },
  {
    path: 'admin/vendors',
    canActivate: [adminGuard],
    loadComponent: () =>
      import('./features/admin/admin-vendors').then(m => m.AdminVendorsComponent),
  },
  { path: '**', redirectTo: '' },
];
