import { ComponentFixture, TestBed } from '@angular/core/testing';

import { GridRenderingProposalComponent } from './grid-rendering-proposal.component';

describe('GridRenderingProposalComponent', () => {
  let component: GridRenderingProposalComponent;
  let fixture: ComponentFixture<GridRenderingProposalComponent>;

  beforeEach(async () => {
    await TestBed.configureTestingModule({
      declarations: [GridRenderingProposalComponent]
    })
    .compileComponents();
    
    fixture = TestBed.createComponent(GridRenderingProposalComponent);
    component = fixture.componentInstance;
    fixture.detectChanges();
  });

  it('should create', () => {
    expect(component).toBeTruthy();
  });
});
