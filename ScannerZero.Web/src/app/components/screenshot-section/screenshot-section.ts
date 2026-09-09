import { isPlatformBrowser } from '@angular/common';
import {
  AfterContentInit,
  Component,
  ContentChildren,
  DestroyRef,
  ElementRef,
  inject,
  Input,
  OnDestroy,
  PLATFORM_ID,
  QueryList,
  signal
} from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { IconComponent } from '../icon/icon';

export type Screenshot = {
  title: string;
  src: string;
  alt: string;
  text: string;
};

@Component({
  selector: 'sz-screenshot-section',
  standalone: true,
  imports: [IconComponent],
  templateUrl: './screenshot-section.html',
  styleUrl: './screenshot-section.css'
})
export class ScreenshotSectionComponent implements AfterContentInit, OnDestroy {
  private readonly destroyRef = inject(DestroyRef);
  private readonly isBrowser = isPlatformBrowser(inject(PLATFORM_ID));
  private screenshotObserver?: IntersectionObserver;

  @Input({ required: true }) screenshots: Screenshot[] = [];

  @ContentChildren('screenshotStep', { descendants: true })
  private readonly screenshotSteps?: QueryList<ElementRef<HTMLElement>>;

  readonly selectedIndex = signal(0);

  ngAfterContentInit(): void {
    if (!this.isBrowser) {
      return;
    }

    queueMicrotask(() => this.observeScreenshotSteps());
    this.screenshotSteps?.changes
      .pipe(takeUntilDestroyed(this.destroyRef))
      .subscribe(() => this.observeScreenshotSteps());
  }

  ngOnDestroy(): void {
    this.screenshotObserver?.disconnect();
  }

  selectPrevious(): void {
    this.selectOffset(-1);
  }

  selectNext(): void {
    this.selectOffset(1);
  }

  private selectOffset(offset: number): void {
    if (this.screenshots.length === 0) {
      return;
    }

    const nextIndex = (this.selectedIndex() + offset + this.screenshots.length) % this.screenshots.length;
    this.selectedIndex.set(nextIndex);
  }

  private observeScreenshotSteps(): void {
    const steps = this.screenshotSteps?.toArray() ?? [];

    if (steps.length === 0) {
      return;
    }

    this.screenshotObserver?.disconnect();
    this.screenshotObserver = new IntersectionObserver((entries) => {
      const activeEntry = entries
        .filter((entry) => entry.isIntersecting)
        .sort((first, second) => second.intersectionRatio - first.intersectionRatio)[0];

      if (!activeEntry) {
        return;
      }

      const index = Number((activeEntry.target as HTMLElement).dataset['screenshotIndex']);

      if (Number.isInteger(index)) {
        this.selectedIndex.set(index);
      }
    }, {
      root: null,
      rootMargin: '-35% 0px -35% 0px',
      threshold: [0, 0.25, 0.5, 0.75, 1]
    });

    for (const step of steps) {
      this.screenshotObserver.observe(step.nativeElement);
    }
  }
}
