import { Component, OnInit, inject, signal, computed } from '@angular/core';
import { FormBuilder, Validators, ReactiveFormsModule } from '@angular/forms';
import { ProfileService } from '../../core/services/profile.service';
import { AuthService } from '../../core/services/auth.service';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner';
import type { UserProfile } from '../../shared/models/user.model';

@Component({
  selector: 'app-profile',
  imports: [ReactiveFormsModule, LoadingSpinnerComponent],
  templateUrl: './profile.html',
  styleUrl: './profile.css',
})
export class ProfileComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly profileService = inject(ProfileService);
  readonly auth = inject(AuthService);

  readonly profile = signal<UserProfile | null>(null);
  readonly loading = signal(true);
  readonly saving = signal(false);
  readonly error = signal<string | null>(null);
  readonly saved = signal(false);

  readonly form = this.fb.nonNullable.group({
    fullName: ['', [Validators.required, Validators.minLength(2)]],
    phoneNumber: [''],
    address: [''],
  });

  readonly displayEmail = computed(
    () => this.profile()?.email ?? this.auth.email() ?? ''
  );

  readonly initials = computed(() => {
    const name = (this.profile()?.fullName ?? this.auth.displayName() ?? '').trim();
    if (!name) return '?';
    const parts = name.split(/\s+/);
    if (parts.length >= 2) return (parts[0][0] + parts[parts.length - 1][0]).toUpperCase();
    return parts[0][0].toUpperCase();
  });

  async ngOnInit(): Promise<void> {
    try {
      const p = await this.profileService.getProfile();
      this.profile.set(p);
      this.form.patchValue({
        fullName: p.fullName,
        phoneNumber: p.phoneNumber ?? '',
        address: p.address ?? '',
      });
    } catch (err: unknown) {
      const status = (err as { status?: number }).status;
      // 404 = no profile created yet; 401 after retry = rare edge case
      // Both cases: show empty form so the user can save/create their profile
      if (status !== 404 && status !== 401) {
        this.error.set((err as Error).message);
      }
    } finally {
      this.loading.set(false);
    }
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
        p ? { ...p, fullName } : { userId: '', email: this.auth.email() ?? '', fullName }
      );
      this.saved.set(true);
      setTimeout(() => this.saved.set(false), 4000);
    } catch (err: unknown) {
      this.error.set((err as Error).message);
    } finally {
      this.saving.set(false);
    }
  }
}
