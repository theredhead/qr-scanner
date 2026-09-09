import { Component, Input } from '@angular/core';

@Component({
  selector: 'sz-hero-section',
  standalone: true,
  templateUrl: './hero-section.html',
  styleUrl: './hero-section.css'
})
export class HeroSectionComponent {
  @Input({ required: true }) releasesUrl = '';
  @Input({ required: true }) repositoryUrl = '';
}
