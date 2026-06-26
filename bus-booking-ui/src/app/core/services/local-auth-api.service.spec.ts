import { TestBed } from '@angular/core/testing';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { provideHttpClient } from '@angular/common/http';
import { LocalAuthApiService } from './local-auth-api.service';

describe('LocalAuthApiService', () => {
  let service: LocalAuthApiService;
  let http: HttpTestingController;

  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [LocalAuthApiService, provideHttpClient(), provideHttpClientTesting()],
    });
    service = TestBed.inject(LocalAuthApiService);
    http    = TestBed.inject(HttpTestingController);
  });

  afterEach(() => http.verify());

  it('register posts to /api/v1/auth/register', async () => {
    const promise = service.register('a@b.com', 'pass', 'Alice');
    const req = http.expectOne('/api/v1/auth/register');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ email: 'a@b.com', password: 'pass', displayName: 'Alice' });
    req.flush({ message: 'ok', userId: '123' });
    const res = await promise;
    expect(res.message).toBe('ok');
  });

  it('login posts to /api/v1/auth/login with credentials', async () => {
    const promise = service.login('a@b.com', 'pass');
    const req = http.expectOne('/api/v1/auth/login');
    expect(req.request.method).toBe('POST');
    expect(req.request.withCredentials).toBeTrue();
    req.flush({ accessToken: 'tok', expiresIn: 900, tokenType: 'Bearer' });
    const res = await promise;
    expect(res.accessToken).toBe('tok');
  });

  it('refresh posts to /api/v1/auth/refresh with credentials', async () => {
    const promise = service.refresh();
    const req = http.expectOne('/api/v1/auth/refresh');
    expect(req.request.method).toBe('POST');
    expect(req.request.withCredentials).toBeTrue();
    req.flush({ accessToken: 'new-tok', expiresIn: 900, tokenType: 'Bearer' });
    const res = await promise;
    expect(res.accessToken).toBe('new-tok');
  });

  it('logout posts to /api/v1/auth/logout with credentials', async () => {
    const promise = service.logout();
    const req = http.expectOne('/api/v1/auth/logout');
    expect(req.request.method).toBe('POST');
    expect(req.request.withCredentials).toBeTrue();
    req.flush(null);
    await expectAsync(promise).toBeResolved();
  });

  it('verifyEmail gets /api/v1/auth/verify-email with encoded token', async () => {
    const promise = service.verifyEmail('my+token=value');
    const req = http.expectOne(r => r.url.startsWith('/api/v1/auth/verify-email'));
    expect(req.request.method).toBe('GET');
    expect(req.request.urlWithParams).toContain('token=');
    req.flush({ message: 'verified' });
    const res = await promise;
    expect(res.message).toBe('verified');
  });

  it('forgotPassword posts to /api/v1/auth/forgot-password', async () => {
    const promise = service.forgotPassword('a@b.com');
    const req = http.expectOne('/api/v1/auth/forgot-password');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ email: 'a@b.com' });
    req.flush({ message: 'sent' });
    await expectAsync(promise).toBeResolved();
  });

  it('resetPassword posts to /api/v1/auth/reset-password', async () => {
    const promise = service.resetPassword('tok', 'NewPass1!');
    const req = http.expectOne('/api/v1/auth/reset-password');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ token: 'tok', newPassword: 'NewPass1!' });
    req.flush({ message: 'reset' });
    await expectAsync(promise).toBeResolved();
  });

  it('getLinkedAccounts gets /api/v1/users/me/linked-accounts', async () => {
    const promise = service.getLinkedAccounts();
    const req = http.expectOne('/api/v1/users/me/linked-accounts');
    expect(req.request.method).toBe('GET');
    req.flush([{ provider: 'Entra', linkedAt: '2026-06-01T00:00:00Z' }]);
    const res = await promise;
    expect(res.length).toBe(1);
    expect(res[0].provider).toBe('Entra');
  });

  it('linkLocal posts to /api/v1/users/me/link-local', async () => {
    const promise = service.linkLocal('MyNewPassword1!');
    const req = http.expectOne('/api/v1/users/me/link-local');
    expect(req.request.method).toBe('POST');
    expect(req.request.body).toEqual({ password: 'MyNewPassword1!' });
    req.flush(null);
    await expectAsync(promise).toBeResolved();
  });

  it('unlinkProvider deletes /api/v1/users/me/linked-accounts/{provider}', async () => {
    const promise = service.unlinkProvider('Local');
    const req = http.expectOne('/api/v1/users/me/linked-accounts/Local');
    expect(req.request.method).toBe('DELETE');
    req.flush(null);
    await expectAsync(promise).toBeResolved();
  });
});
