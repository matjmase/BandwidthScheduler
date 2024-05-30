import { ComponentFixture, TestBed } from '@angular/core/testing';

import { GridLegendReadOnlyComponent } from './grid-legend-read-only.component';

describe('GridLegendReadOnlyComponent', () => {
  let component: GridLegendReadOnlyComponent;
  let fixture: ComponentFixture<GridLegendReadOnlyComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [GridLegendReadOnlyComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(GridLegendReadOnlyComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
