import { Component, OnInit, inject, input, signal } from '@angular/core';
import { Router, RouterLink } from '@angular/router';
import { ScheduleService } from '../../core/services/schedule.service';
import { FeedbackService } from '../../core/services/feedback.service';
import { LoadingSpinnerComponent } from '../../shared/components/loading-spinner/loading-spinner';
import { StatusBadgeComponent } from '../../shared/components/status-badge/status-badge';
import type { Schedule } from '../../shared/models/schedule.model';
import type { FeedbackStats } from '../../shared/models/feedback.model';

@Component({
  selector: 'app-schedule-detail',
  imports: [RouterLink, LoadingSpinnerComponent, StatusBadgeComponent],
  templateUrl: './schedule-detail.html',
})
export class ScheduleDetailComponent implements OnInit {
  readonly id = input.required<string>();
  private readonly router = inject(Router);
  private readonly scheduleService = inject(ScheduleService);
  private readonly feedbackService = inject(FeedbackService);

  readonly schedule = signal<Schedule | null>(null);
  readonly stats = signal<FeedbackStats | null>(null);
  readonly loading = signal(true);
  readonly error = signal<string | null>(null);

  async ngOnInit(): Promise<void> {
    try {
      const s = await this.scheduleService.getById(this.id());
      this.schedule.set(s);
      try {
        const st = await this.feedbackService.getScheduleStats(this.id());
        this.stats.set(st);
      } catch { /* stats optional */ }
    } catch (err: unknown) {
      this.error.set((err as Error).message);
    } finally {
      this.loading.set(false);
    }
  }

  book(): void {
    this.router.navigate(['/book', this.id()]);
  }

  formatTime(t: string): string {
    if (!t) return '';
    if (t.includes('T') || t.length > 8) {
      return new Date(t).toLocaleTimeString('en-IN', { hour: '2-digit', minute: '2-digit', hour12: true });
    }
    const [h, m] = t.split(':').map(Number);
    const suffix = h >= 12 ? 'PM' : 'AM';
    const hour12 = h % 12 || 12;
    return `${hour12}:${String(m).padStart(2, '0')} ${suffix}`;
  }

  stars(avg: number): string {
    return '★'.repeat(Math.round(avg)) + '☆'.repeat(5 - Math.round(avg));
  }
}
