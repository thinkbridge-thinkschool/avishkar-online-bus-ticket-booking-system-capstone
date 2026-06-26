import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { FormBuilder, Validators, ReactiveFormsModule, FormsModule } from '@angular/forms';
import { DatePipe } from '@angular/common';
import { ProfileService } from '../../core/services/profile.service';
import { AuthService } from '../../core/services/auth.service';
import { LocalAuthApiService } from '../../core/services/local-auth-api.service';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner';
import type { UserProfile } from '../../shared/models/user.model';
import { environment } from '../../../environments/environment';

@Component({
  selector: 'app-profile',
  imports: [ReactiveFormsModule, FormsModule, DatePipe, LoadingSpinnerComponent],
  templateUrl: './profile.html',
  styleUrl: './profile.css',
})
export class ProfileComponent implements OnInit {
  private readonly fb      = inject(FormBuilder);
  private readonly profileService = inject(ProfileService);
  private readonly api     = inject(LocalAuthApiService);
  readonly auth            = inject(AuthService);

  // ── Profile form state ──────────────────────────────────────────────────────
  readonly profile  = signal<UserProfile | null>(null);
  readonly loading  = signal(true);
  readonly saving   = signal(false);
  readonly error    = signal<string | null>(null);
  readonly saved    = signal(false);

  readonly form = this.fb.nonNullable.group({
    fullName:    ['', [Validators.required, Validators.minLength(2)]],
    phoneNumber: [''],
    address:     [''],
  });

  readonly displayEmail = computed(
    () => this.profile()?.email ?? this.auth.email() ?? '',
  );

  readonly initials = computed(() => {
    const name = (this.profile()?.fullName ?? this.auth.displayName() ?? '').trim();
    if (!name) return '?';
    const parts = name.split(/\s+/);
    if (parts.length >= 2) return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
    return parts[0][0].toUpperCase();
  });

  // ── Linked accounts state ───────────────────────────────────────────────────
  readonly localAuthEnabled = environment.localAuthEnabled;

  readonly linkedAccounts  = signal<{ provider: string; linkedAt: string }[]>([]);
  readonly linkingLocal    = signal(false);
  readonly linkPassword    = signal('');
  readonly linkLoading     = signal(false);
  readonly linkError       = signal<string | null>(null);
  readonly linkSuccess     = signal(false);
  readonly unlinkLoading   = signal<string | null>(null); // holds the provider being unlinked
  readonly unlinkError     = signal<string | null>(null);

  readonly hasLocalAuth = computed(() =>
    this.linkedAccounts().some(a => a.provider.toLowerCase() === 'local'),
  );

  async ngOnInit(): Promise<void> {
    try {
      const p = await this.profileService.getProfile();
      this.profile.set(p);
      this.form.patchValue({
        fullName:    p.fullName,
        phoneNumber: p.phoneNumber ?? '',
        address:     p.address ?? '',
      });
    } catch (err: unknown) {
      const status = (err as { status?: number }).status;
      if (status !== 404 && status !== 401) {
        this.error.set((err as Error).message);
      }
    } finally {
      this.loading.set(false);
    }
    await this.loadLinkedAccounts();
  }

  async save(): Promise<void> {
    this.form.markAllAsTouched();
    if (this.form.invalid) return;
    this.saving.set(true);
    this.error.set(null);
    this.saved.set(false);
    try {
      await this.profileService.updateProfile(this.form.getRawValue());
      const fullName = this.form.controls.fullName.value;
      this.profile.update(p =>
        p ? { ...p, fullName } : { userId: '', email: this.auth.email() ?? '', fullName },
      );
      this.saved.set(true);
      setTimeout(() => this.saved.set(false), 4000);
    } catch (err: unknown) {
      this.error.set((err as Error).message);
    } finally {
      this.saving.set(false);
    }
  }

  // ── Linked accounts ─────────────────────────────────────────────────────────

  private async loadLinkedAccounts(): Promise<void> {
    if (!this.localAuthEnabled) return;
    try {
      const accounts = await this.api.getLinkedAccounts();
      this.linkedAccounts.set(accounts);
    } catch {
      // Non-critical — linked accounts section is hidden on error.
    }
  }

  async linkLocal(): Promise<void> {
    const pwd = this.linkPassword();
    if (pwd.length < 8) {
      this.linkError.set('Password must be at least 8 characters.');
      return;
    }
    this.linkLoading.set(true);
    this.linkError.set(null);
    try {
      await this.api.linkLocal(pwd);
      this.linkSuccess.set(true);
      this.linkingLocal.set(false);
      this.linkPassword.set('');
      await this.loadLinkedAccounts();
      setTimeout(() => this.linkSuccess.set(false), 4000);
    } catch (err: unknown) {
      const status = (err as { status?: number }).status;
      this.linkError.set(
        status === 409 ? 'Local credentials are already linked.' : 'Failed to link local auth.',
      );
    } finally {
      this.linkLoading.set(false);
    }
  }

  async unlinkProvider(provider: string): Promise<void> {
    if (this.linkedAccounts().length <= 1) {
      this.unlinkError.set('Cannot remove the only sign-in method.');
      return;
    }
    this.unlinkLoading.set(provider);
    this.unlinkError.set(null);
    try {
      await this.api.unlinkProvider(provider);
      await this.loadLinkedAccounts();
      if (provider.toLowerCase() === 'local') {
        // Local session tokens are revoked server-side; let auth service know
        this.auth.logout();
      }
    } catch (err: unknown) {
      this.unlinkError.set('Failed to unlink provider. Please try again.');
    } finally {
      this.unlinkLoading.set(null);
    }
  }

  updateLinkPassword(event: Event): void {
    this.linkPassword.set((event.target as HTMLInputElement).value);
  }
}
