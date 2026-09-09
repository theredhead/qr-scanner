import { Component, Input } from '@angular/core';

export type Feature = {
  title: string;
  text: string;
};

@Component({
  selector: 'sz-features-section',
  standalone: true,
  templateUrl: './features-section.html',
  styleUrl: './features-section.css'
})
export class FeaturesSectionComponent {
  @Input({ required: true }) features: Feature[] = [];
}
