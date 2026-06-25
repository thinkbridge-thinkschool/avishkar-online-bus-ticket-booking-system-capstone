import { TestBed } from '@angular/core/testing';
import { MsalService } from '@azure/msal-angular';
import { AuthService } from './auth.service';
import type { AccountInfo } from '@azure/msal-browser';

/** Minimal stub that satisfies the MsalService interface used by AuthService. */
function makeMsalStub(accounts: AccountInfo[] = []) {
  const instance = {
    initialize: jasmine.createSpy('initialize').and.returnValue(Promise.resolve()),
    handleRedirectPromise: jasmine.createSpy('handleRedirectPromise').and.returnValue(
      Promise.resolve(null)
    ),
    getAllAccounts: jasmine.createSpy('getAllAccounts').and.returnValue(accounts),
    acquireTokenSilent: jasmine.createSpy('acquireTokenSilent'),
    acquireTokenRedirect: jasmine.createSpy('acquireTokenRedirect'),
    loginRedirect: jasmine.createSpy('loginRedirect'),
    logoutRedirect: jasmine.createSpy('logoutRedirect'),
  };
  return { instance } as unknown as MsalService;
}

function makeAccount(overrides: Partial<AccountInfo> = {}): AccountInfo {
  return {
    homeAccountId: 'home-id',
    environment: 'login.microsoftonline.com',
    tenantId: 'tenant-id',
    username: 'user@example.com',
    localAccountId: 'local-id',
    name: 'Test User',
    idTokenClaims: { roles: ['BusBooking.Vendor'] },
    ...overrides,
  } as AccountInfo;
}

describe('AuthService', () => {
  let service: AuthService;

  describe('when no account is signed in', () => {
    beforeEach(() => {
      TestBed.configureTestingModule({
        providers: [
          AuthService,
          { provide: MsalService, useValue: makeMsalStub([]) },
        ],
      });
      service = TestBed.inject(AuthService);
    });

    it('isAuthenticated should be false', () => {
      expect(service.isAuthenticated()).toBeFalse();
    });

    it('isVendor should be false', () => {
      expect(service.isVendor()).toBeFalse();
    });

    it('isAdmin should be false', () => {
      expect(service.isAdmin()).toBeFalse();
    });
  });

  describe('when a Vendor account is signed in', () => {
    beforeEach(async () => {
      const account = makeAccount({ idTokenClaims: { roles: ['BusBooking.Vendor'] } });
      TestBed.configureTestingModule({
        providers: [
          AuthService,
          { provide: MsalService, useValue: makeMsalStub([account]) },
        ],
      });
      service = TestBed.inject(AuthService);
      await service.initialize();
    });

    it('isAuthenticated should be true', () => {
      expect(service.isAuthenticated()).toBeTrue();
    });

    it('isVendor should be true', () => {
      expect(service.isVendor()).toBeTrue();
    });

    it('isAdmin should be false', () => {
      expect(service.isAdmin()).toBeFalse();
    });
  });

  describe('when an Admin account is signed in', () => {
    beforeEach(async () => {
      const account = makeAccount({ idTokenClaims: { roles: ['BusBooking.Admin'] } });
      TestBed.configureTestingModule({
        providers: [
          AuthService,
          { provide: MsalService, useValue: makeMsalStub([account]) },
        ],
      });
      service = TestBed.inject(AuthService);
      await service.initialize();
    });

    it('isAdmin should be true', () => {
      expect(service.isAdmin()).toBeTrue();
    });

    it('isVendor should be false', () => {
      expect(service.isVendor()).toBeFalse();
    });
  });
});
