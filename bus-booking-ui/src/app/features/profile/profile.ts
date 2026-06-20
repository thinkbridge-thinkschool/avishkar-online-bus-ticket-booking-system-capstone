import { Component, OnInit, inject, signal } from '@angular/core';
import { FormBuilder, Validators, ReactiveFormsModule } from '@angular/forms';
import { ProfileService } from '../../core/services/profile.service';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner';
import type { UserProfile } from '../../shared/models/user.model';

@Component({
  selector: 'app-profile',
  imports: [ReactiveFormsModule, LoadingSpinnerComponent],
  templateUrl: './profile.html',
})
export class ProfileComponent implements OnInit {
  private readonly fb = inject(FormBuilder);
  private readonly profileService = inject(ProfileService);

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

  async ngOnInit(): Promise<void> {
    try {
      const p = await this.profileService.getProfile();
      this.profile.set(p);
      this.form.patchValue({ fullName: p.fullName, phoneNumber: p.phoneNumber ?? '', address: p.address ?? '' });
    } catch (err: unknown) {
      this.error.set((err as Error).message);
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
      this.saved.set(true);
    } catch (err: unknown) {
      this.error.set((err as Error).message);
    } finally {
      this.saving.set(false);
    }
  }
}
